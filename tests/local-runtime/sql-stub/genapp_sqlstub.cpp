// Minimal GIXSQL-runtime-compatible library, backed directly by libpq.
//
// Why this exists: GixSQL's own prebuilt Windows/x64/mingw runtime has two
// confirmed upstream bugs (missing fmt symbols, a crash inside its pgsql
// driver on connect) that block it entirely - see docs/local-cics-runtime-plan.md.
// gixpp (the *preprocessor*) is unaffected and works fine; it is still used
// to translate `EXEC SQL ... END-EXEC` into `CALL "GIXSQLxxx" ...` statements.
// This file implements just the subset of that runtime API that GenApp's own
// COBOL programs actually generate (confirmed by preprocessing the real
// base/src/*.cbl files, not guessed) - not a general GixSQL replacement.
//
// Host variable type codes seen in practice (from gixpp output, not GixSQL's
// source): 16 = alphanumeric (PIC X, space-padded, fixed length), 23 = binary
// COMP integer (big-endian, confirmed empirically - see byteorder.cbl probe).
// GenApp never uses packed-decimal (COMP-3) or zoned-display numeric host
// variables in SQL - every numeric key is explicitly converted to COMP first.

#include <libpq-fe.h>
#include <cstdint>
#include <cstring>
#include <cstdio>
#include <string>
#include <vector>
#include <map>
#include <algorithm>
#include <regex>

#pragma pack(push, 1)
struct SQLCA_T {
    char    sqlcaid[8];
    int32_t sqlcabc;
    int32_t sqlcode;
    int16_t sqlerrml;
    char    sqlerrmc[70];
    char    sqlerrp[8];
    int32_t sqlerrd[6];
    char    sqlwarn[8];
    char    sqlext[8];
};
#pragma pack(pop)

namespace {

PGconn* g_conn = nullptr;

struct ParamSlot {
    std::string value;
    int   type;
    int   length;
    void* data;  // raw buffer, needed for the SET :var = expr special case below
};
std::vector<ParamSlot> g_params;

struct ResultSlot {
    int   type;
    int   length;
    void* buf;
};
std::vector<ResultSlot> g_results;

struct CursorState {
    std::string sql;
    PGresult*   res = nullptr;
    int         next_row = 0;
};
std::map<std::string, CursorState> g_cursors;

std::string trim_trailing_spaces(const char* data, int len) {
    int end = len;
    while (end > 0 && data[end - 1] == ' ') end--;
    return std::string(data, end);
}

std::string cstr_from_fixed(const char* data, int max_len) {
    int len = 0;
    while (len < max_len && data[len] != '\0') len++;
    return std::string(data, len);
}

// GnuCOBOL's default binary representation is big-endian (mainframe-compatible),
// confirmed empirically for this toolchain - do not assume host byte order.
int32_t decode_comp32_be(const unsigned char* b) {
    return (int32_t)((uint32_t)b[0] << 24 | (uint32_t)b[1] << 16 |
                      (uint32_t)b[2] << 8 | (uint32_t)b[3]);
}

void encode_comp32_be(unsigned char* b, int32_t v) {
    b[0] = (unsigned char)((uint32_t)v >> 24);
    b[1] = (unsigned char)((uint32_t)v >> 16);
    b[2] = (unsigned char)((uint32_t)v >> 8);
    b[3] = (unsigned char)((uint32_t)v);
}

// COMP-3 packed decimal: 2 BCD digits/byte, last nibble is the sign
// (0xC/0xF = positive, 0xD = negative). `digits` is the PICTURE digit count,
// not the byte count - GenApp itself never uses this type (confirmed by
// preprocessing the real .cbl files), but the generic GixSQL test programs
// used to validate this stub do, so it's handled for completeness.
// Nibble layout is always: [optional zero pad nibble(s)] digit[0..digits-1] sign.
// The sign nibble is always the low nibble of the last byte - digit nibbles are
// the `digits` nibbles immediately before it, regardless of whether `digits` is
// even or odd (an even digit count needs one leading pad nibble to fill whole
// bytes; an odd one doesn't). Indexing by nibble position (not byte-at-a-time)
// avoids getting that parity case wrong.
int nibble_at(const unsigned char* b, int idx) {
    unsigned char byte = b[idx / 2];
    return (idx % 2 == 0) ? ((byte >> 4) & 0xF) : (byte & 0xF);
}

std::string decode_comp3(const unsigned char* b, int digits) {
    int nbytes = digits / 2 + 1;
    int total_nibbles = nbytes * 2;
    int sign_nibble = nibble_at(b, total_nibbles - 1);
    bool negative = (sign_nibble == 0xD || sign_nibble == 0xB);
    std::string out;
    for (int i = total_nibbles - 1 - digits; i < total_nibbles - 1; i++) {
        out += static_cast<char>('0' + nibble_at(b, i));
    }
    size_t first_nonzero = out.find_first_not_of('0');
    if (first_nonzero == std::string::npos) first_nonzero = out.size() - 1;
    out = out.substr(first_nonzero);
    return negative ? "-" + out : out;
}

std::string encode_param(int type, int length, void* data) {
    if (type == 23) {
        int32_t v = decode_comp32_be(reinterpret_cast<unsigned char*>(data));
        return std::to_string(v);
    }
    if (type == 9) {
        return decode_comp3(reinterpret_cast<unsigned char*>(data), length);
    }
    if (type == 1) {
        // zoned-display numeric: bytes already are ASCII digits, no padding to trim
        return std::string(reinterpret_cast<char*>(data), length);
    }
    // type 16 (and fallback for anything else): alphanumeric, space-padded
    return trim_trailing_spaces(reinterpret_cast<char*>(data), length);
}

void encode_comp3(unsigned char* b, int digits, int64_t v) {
    int nbytes = digits / 2 + 1;
    int total_nibbles = nbytes * 2;
    bool negative = v < 0;
    uint64_t mag = negative ? (uint64_t)(-v) : (uint64_t)v;
    std::string ds = std::to_string(mag);
    if ((int)ds.size() > digits) ds = ds.substr(ds.size() - digits);  // truncate, shouldn't happen
    while ((int)ds.size() < digits) ds = "0" + ds;

    std::vector<int> nibbles(total_nibbles, 0);  // leading pad nibble(s) stay 0
    for (int i = 0; i < digits; i++) {
        nibbles[total_nibbles - 1 - digits + i] = ds[i] - '0';
    }
    nibbles[total_nibbles - 1] = negative ? 0xD : 0xC;
    for (int i = 0; i < nbytes; i++) {
        b[i] = (unsigned char)((nibbles[i * 2] << 4) | nibbles[i * 2 + 1]);
    }
}

void decode_into_result(const ResultSlot& slot, const char* pg_text) {
    if (slot.type == 23) {
        int32_t v = pg_text && *pg_text ? (int32_t)atoll(pg_text) : 0;
        encode_comp32_be(reinterpret_cast<unsigned char*>(slot.buf), v);
        return;
    }
    if (slot.type == 9) {
        int64_t v = pg_text && *pg_text ? atoll(pg_text) : 0;
        encode_comp3(reinterpret_cast<unsigned char*>(slot.buf), slot.length, v);
        return;
    }
    if (slot.type == 1) {
        // zoned-display numeric: right-justify, zero-pad to `length` digits
        std::string v = pg_text ? pg_text : "";
        bool negative = !v.empty() && v[0] == '-';
        if (negative) v = v.substr(1);
        if ((int)v.size() > slot.length) v = v.substr(v.size() - slot.length);
        while ((int)v.size() < slot.length) v = "0" + v;
        memcpy(slot.buf, v.data(), slot.length);
        return;
    }
    // type 16: copy text left-justified, space-pad/truncate to `length`
    char* dst = reinterpret_cast<char*>(slot.buf);
    size_t src_len = pg_text ? strlen(pg_text) : 0;
    size_t copy_len = std::min(src_len, (size_t)slot.length);
    memcpy(dst, pg_text, copy_len);
    if ((int)copy_len < slot.length) {
        memset(dst + copy_len, ' ', slot.length - copy_len);
    }
}

void bind_result_row(PGresult* res, int row) {
    int ncols = PQnfields(res);
    for (size_t i = 0; i < g_results.size() && (int)i < ncols; i++) {
        decode_into_result(g_results[i], PQgetisnull(res, row, (int)i)
                                              ? ""
                                              : PQgetvalue(res, row, (int)i));
    }
}

void sqlca_ok(SQLCA_T* ca) {
    memset(ca->sqlcaid, ' ', sizeof(ca->sqlcaid));
    ca->sqlcode = 0;
    ca->sqlerrml = 0;
    memset(ca->sqlerrmc, ' ', sizeof(ca->sqlerrmc));
}

void sqlca_error(SQLCA_T* ca, int32_t code, const char* msg) {
    if (std::getenv("GENAPP_SQL_DEBUG")) {
        fprintf(stderr, "[genapp_sqlstub] SQLCODE=%d: %s\n", code, msg ? msg : "(null)");
    }
    ca->sqlcode = code;
    std::string m = msg ? msg : "";
    // libpq error messages often end in a newline and can be long; SQLERRMC is 70 bytes
    while (!m.empty() && (m.back() == '\n' || m.back() == '\r')) m.pop_back();
    if (m.size() > sizeof(ca->sqlerrmc)) m.resize(sizeof(ca->sqlerrmc));
    ca->sqlerrml = (int16_t)m.size();
    memset(ca->sqlerrmc, ' ', sizeof(ca->sqlerrmc));
    memcpy(ca->sqlerrmc, m.data(), m.size());
}

// "pgsql://host:port/dbname[?options]" -> libpq keyword/value string.
// GenApp/gixpp only ever produce this exact shape - no need for a general parser.
std::string build_conninfo(const std::string& datasrc, const std::string& user,
                            const std::string& pwd) {
    std::string rest = datasrc;
    const std::string prefix = "pgsql://";
    if (rest.rfind(prefix, 0) == 0) rest = rest.substr(prefix.size());

    std::string hostport, dbname;
    auto slash = rest.find('/');
    if (slash != std::string::npos) {
        hostport = rest.substr(0, slash);
        dbname = rest.substr(slash + 1);
        auto qmark = dbname.find('?');
        if (qmark != std::string::npos) dbname = dbname.substr(0, qmark);
    } else {
        hostport = rest;
    }
    std::string host = hostport, port = "5432";
    auto colon = hostport.find(':');
    if (colon != std::string::npos) {
        host = hostport.substr(0, colon);
        port = hostport.substr(colon + 1);
    }

    std::string conninfo = "host=" + host + " port=" + port + " dbname=" + dbname;
    if (!user.empty()) conninfo += " user=" + user;
    if (!pwd.empty()) conninfo += " password=" + pwd;
    return conninfo;
}

// Real CICS+Db2 programs almost never issue EXEC SQL CONNECT themselves -
// the connection is established implicitly by CICS's Db2 attachment
// facility (DB2CONN=YES) before the program ever runs, which is exactly
// why lgacdb01.cbl (and most of base/src's *db01/*db02 programs) has no
// CONNECT statement at all. Mirror that by auto-connecting on first real
// use from the same DATASRC/_USR/_PWD env vars an explicit CONNECT would
// have used, rather than requiring every call chain to start with one.
void ensure_connected() {
    if (g_conn && PQstatus(g_conn) == CONNECTION_OK) return;
    const char* ds = std::getenv("DATASRC");
    const char* u = std::getenv("DATASRC_USR");
    const char* p = std::getenv("DATASRC_PWD");
    std::string conninfo = build_conninfo(ds ? ds : "", u ? u : "", p ? p : "");
    if (g_conn) PQfinish(g_conn);
    g_conn = PQconnectdb(conninfo.c_str());
}

}  // namespace

extern "C" {

int GIXSQLStartSQL() {
    g_params.clear();
    g_results.clear();
    return 0;
}

int GIXSQLEndSQL() { return 0; }

int GIXSQLConnect(void* sqlca_v, char* datasrc, int datasrc_len, char*, int,
                   char*, int, char* user, int user_len, char* pwd, int pwd_len) {
    SQLCA_T* ca = reinterpret_cast<SQLCA_T*>(sqlca_v);
    std::string ds = trim_trailing_spaces(datasrc, datasrc_len);
    std::string u = trim_trailing_spaces(user, user_len);
    std::string p = trim_trailing_spaces(pwd, pwd_len);

    if (g_conn) {
        PQfinish(g_conn);
        g_conn = nullptr;
    }
    std::string conninfo = build_conninfo(ds, u, p);
    g_conn = PQconnectdb(conninfo.c_str());
    if (PQstatus(g_conn) != CONNECTION_OK) {
        sqlca_error(ca, -100, PQerrorMessage(g_conn));
        return -1;
    }
    sqlca_ok(ca);
    return 0;
}

int GIXSQLConnectReset(void* sqlca_v, char*, int) {
    if (g_conn) {
        PQfinish(g_conn);
        g_conn = nullptr;
    }
    sqlca_ok(reinterpret_cast<SQLCA_T*>(sqlca_v));
    return 0;
}

int GIXSQLSetSQLParams(int type, int length, int /*scale*/, int /*flags*/, void* data,
                        void* /*null_ind*/) {
    g_params.push_back({encode_param(type, length, data), type, length, data});
    return 0;
}

int GIXSQLSetResultParams(int type, int length, int /*scale*/, int /*flags*/, void* data,
                           void* /*null_ind*/) {
    g_results.push_back({type, length, data});
    return 0;
}

int GIXSQLExec(void* sqlca_v, char*, int, char* sql) {
    SQLCA_T* ca = reinterpret_cast<SQLCA_T*>(sqlca_v);
    ensure_connected();
    PGresult* res = PQexec(g_conn, sql);
    ExecStatusType st = PQresultStatus(res);
    if (st != PGRES_COMMAND_OK && st != PGRES_TUPLES_OK) {
        sqlca_error(ca, -1, PQresultErrorMessage(res));
        PQclear(res);
        return -1;
    }
    if (st == PGRES_TUPLES_OK && PQntuples(res) > 0 && !g_results.empty()) {
        bind_result_row(res, 0);
    }
    sqlca_ok(ca);
    PQclear(res);
    return 0;
}

// GixSQL's own embedded-SQL dialect supports `EXEC SQL SET :var = expr
// END-EXEC` (used by GenApp for Db2's IDENTITY_VAL_LOCAL() right after an
// auto-numbered INSERT); gixpp compiles it into a statement that literally
// reads "SET $1 = <expr>", naming the first bound parameter as the
// assignment *target*. PostgreSQL has no such statement, and even if it
// did, $1 is an input placeholder, not an output - so this can't just be
// forwarded to PQexecParams like a normal statement. Recognize the shape
// and turn it into `SELECT <expr>`, writing the single result back into
// that parameter's own buffer instead of binding it as input.
bool try_exec_set_assignment(SQLCA_T* ca, const char* sql) {
    static const std::regex set_re(R"(^\s*SET\s+\$(\d+)\s*=\s*(.+?)\s*$)",
                                    std::regex::icase);
    std::cmatch m;
    if (!std::regex_match(sql, m, set_re) || g_params.empty()) return false;

    int idx = std::stoi(m[1].str()) - 1;
    std::string expr = m[2].str();
    // Only Db2-ism GenApp relies on here; PostgreSQL's equivalent is the
    // last value generated by a sequence in the current session, which is
    // exactly what the INSERT immediately before this used.
    const std::string db2_fn = "IDENTITY_VAL_LOCAL()";
    size_t p;
    while ((p = expr.find(db2_fn)) != std::string::npos) {
        expr.replace(p, db2_fn.size(), "lastval()");
    }

    PGresult* res = PQexec(g_conn, ("SELECT " + expr).c_str());
    if (PQresultStatus(res) != PGRES_TUPLES_OK || PQntuples(res) == 0) {
        sqlca_error(ca, -1, PQresultErrorMessage(res));
        PQclear(res);
        return true;
    }
    if (idx >= 0 && idx < (int)g_params.size()) {
        ResultSlot rs{g_params[idx].type, g_params[idx].length, g_params[idx].data};
        decode_into_result(rs, PQgetisnull(res, 0, 0) ? "" : PQgetvalue(res, 0, 0));
    }
    PQclear(res);
    sqlca_ok(ca);
    return true;
}

int GIXSQLExecParams(void* sqlca_v, char*, int, char* sql, int /*nparams*/) {
    SQLCA_T* ca = reinterpret_cast<SQLCA_T*>(sqlca_v);
    ensure_connected();
    if (try_exec_set_assignment(ca, sql)) return ca->sqlcode == 0 ? 0 : -1;
    std::vector<const char*> vals;
    vals.reserve(g_params.size());
    for (auto& p : g_params) vals.push_back(p.value.c_str());
    PGresult* res = PQexecParams(g_conn, sql, (int)g_params.size(), nullptr, vals.data(),
                                  nullptr, nullptr, 0);
    ExecStatusType st = PQresultStatus(res);
    if (st != PGRES_COMMAND_OK && st != PGRES_TUPLES_OK) {
        sqlca_error(ca, -1, PQresultErrorMessage(res));
        PQclear(res);
        return -1;
    }
    if (st == PGRES_TUPLES_OK && PQntuples(res) > 0 && !g_results.empty()) {
        bind_result_row(res, 0);
    }
    sqlca_ok(ca);
    PQclear(res);
    return 0;
}

// cobol: CALL "GIXSQLExecSelectIntoOne" USING sqlca connid connid-len sql
//        BY VALUE nparams BY VALUE nresults
// gixpp emits this (instead of GIXSQLExecParams) for a non-cursor
// `SELECT col1,...,colN INTO :h1,...,:hN FROM t WHERE k=:p` - single-row
// select combined with the fetch in one call, no cursor needed. Equivalent
// to ExecParams + fetching the first row into the registered result params,
// with "no row" mapped to SQLCODE 100 (GenApp's own convention, checked
// throughout base/src) rather than treated as an error.
int GIXSQLExecSelectIntoOne(void* sqlca_v, char*, int, char* sql, int /*nparams*/,
                             int /*nresults*/) {
    SQLCA_T* ca = reinterpret_cast<SQLCA_T*>(sqlca_v);
    ensure_connected();
    std::vector<const char*> vals;
    vals.reserve(g_params.size());
    for (auto& p : g_params) vals.push_back(p.value.c_str());
    PGresult* res = PQexecParams(g_conn, sql, (int)g_params.size(), nullptr, vals.data(),
                                  nullptr, nullptr, 0);
    ExecStatusType st = PQresultStatus(res);
    if (st != PGRES_TUPLES_OK) {
        sqlca_error(ca, -1, PQresultErrorMessage(res));
        PQclear(res);
        return -1;
    }
    if (PQntuples(res) == 0) {
        ca->sqlcode = 100;
        ca->sqlerrml = 0;
        PQclear(res);
        return 0;
    }
    bind_result_row(res, 0);
    sqlca_ok(ca);
    PQclear(res);
    return 0;
}

int GIXSQLCursorDeclare(void* sqlca_v, char*, int, char* cursor_name, int, char* sql, int) {
    std::string name = cstr_from_fixed(cursor_name, 256);
    g_cursors[name] = CursorState{std::string(sql), nullptr, 0};
    sqlca_ok(reinterpret_cast<SQLCA_T*>(sqlca_v));
    return 0;
}

int GIXSQLCursorOpen(void* sqlca_v, char* cursor_name) {
    SQLCA_T* ca = reinterpret_cast<SQLCA_T*>(sqlca_v);
    ensure_connected();
    std::string name = cstr_from_fixed(cursor_name, 256);
    auto it = g_cursors.find(name);
    if (it == g_cursors.end()) {
        sqlca_error(ca, -1, "cursor not declared");
        return -1;
    }
    // Emulated (non-streaming) cursor: run the query fully now, buffer rows,
    // fetch from the buffer. Fine for GenApp's record counts; not meant to
    // scale to huge result sets.
    PGresult* res = PQexec(g_conn, it->second.sql.c_str());
    if (PQresultStatus(res) != PGRES_TUPLES_OK) {
        sqlca_error(ca, -1, PQresultErrorMessage(res));
        PQclear(res);
        return -1;
    }
    if (it->second.res) PQclear(it->second.res);
    it->second.res = res;
    it->second.next_row = 0;
    sqlca_ok(ca);
    return 0;
}

int GIXSQLCursorFetchOne(void* sqlca_v, char* cursor_name) {
    SQLCA_T* ca = reinterpret_cast<SQLCA_T*>(sqlca_v);
    std::string name = cstr_from_fixed(cursor_name, 256);
    auto it = g_cursors.find(name);
    if (it == g_cursors.end() || !it->second.res) {
        sqlca_error(ca, -1, "cursor not open");
        return -1;
    }
    if (it->second.next_row >= PQntuples(it->second.res)) {
        ca->sqlcode = 100;  // standard "no more rows" - matches GenApp's own checks
        ca->sqlerrml = 0;
        return 0;
    }
    bind_result_row(it->second.res, it->second.next_row);
    it->second.next_row++;
    sqlca_ok(ca);
    return 0;
}

int GIXSQLCursorClose(void* sqlca_v, char* cursor_name) {
    std::string name = cstr_from_fixed(cursor_name, 256);
    auto it = g_cursors.find(name);
    if (it != g_cursors.end()) {
        if (it->second.res) PQclear(it->second.res);
        g_cursors.erase(it);
    }
    sqlca_ok(reinterpret_cast<SQLCA_T*>(sqlca_v));
    return 0;
}

}  // extern "C"
