// VSAM KSDS emulation for GenApp's two data files (KSDSCUST, KSDSPOLY),
// called from COBOL programs via the CALL statements our CICS preprocessor
// generates in place of `EXEC CICS READ/WRITE/REWRITE/DELETE FILE(...)`.
//
// Record layouts are not guessed - they're read directly off the real
// `EXEC CICS WRITE FILE(...)` calls in base/src/lgacvs01.cbl (KSDSCUST,
// 225 bytes, 10-byte key = the leading CA-CUSTOMER-NUM) and
// base/src/lgapvs01.cbl (KSDSPOLY, 64 bytes, 21-byte key = request-id(1) +
// customer-num(10) + policy-num(10)). See docs/local-cics-runtime-plan.md.
//
// Storage: each file is loaded fully into an in-memory, key-sorted map on
// first use (GenApp's sample datasets are a handful of records - this is a
// test harness, not a production VSAM replacement) and rewritten to a flat
// fixed-length-record file after every mutation. GTEQ/GENERIC browse reads
// map directly onto std::map::lower_bound, since VSAM KSDS and std::map are
// both key-ordered.
//
// Working files live in $GENAPP_VSAM_DIR (KSDSCUST.dat / KSDSPOLY.dat) -
// NOT in base/data/, which stays untouched; seed them by copying
// base/data/ksdscust.txt and ksdspoly.txt there once (same fixed-width
// format, no conversion needed).

#include <cstdint>
#include <cstdlib>
#include <cstring>
#include <fstream>
#include <map>
#include <string>

namespace {

struct FileSpec {
    int record_len;
    int key_len;  // key is always the first key_len bytes of the record
};

const std::map<std::string, FileSpec> kFiles = {
    {"KSDSCUST", {225, 10}},
    {"KSDSPOLY", {64, 21}},
};

struct FileState {
    std::map<std::string, std::string> records;  // key -> full record
    std::string last_read_key;                    // for REWRITE (no RIDFLD)
    bool loaded = false;
};

std::map<std::string, FileState> g_state;  // by file name

std::string data_dir() {
    const char* d = std::getenv("GENAPP_VSAM_DIR");
    return d ? std::string(d) : std::string(".");
}

std::string path_for(const std::string& file_name) {
    return data_dir() + "/" + file_name + ".dat";
}

void load_if_needed(const std::string& file_name, const FileSpec& spec) {
    FileState& st = g_state[file_name];
    if (st.loaded) return;
    st.loaded = true;
    std::ifstream in(path_for(file_name), std::ios::binary);
    if (!in) return;  // no working file yet = empty file, not an error
    std::string buf(spec.record_len, '\0');
    while (in.read(&buf[0], spec.record_len)) {
        st.records[buf.substr(0, spec.key_len)] = buf;
    }
}

void persist(const std::string& file_name) {
    std::ofstream out(path_for(file_name), std::ios::binary | std::ios::trunc);
    for (auto& kv : g_state[file_name].records) {
        out.write(kv.second.data(), kv.second.size());
    }
}

// GnuCOBOL's default binary layout is big-endian (confirmed empirically -
// same probe used for the SQL stub's COMP handling).
void encode_comp32_be(unsigned char* b, int32_t v) {
    b[0] = (unsigned char)((uint32_t)v >> 24);
    b[1] = (unsigned char)((uint32_t)v >> 16);
    b[2] = (unsigned char)((uint32_t)v >> 8);
    b[3] = (unsigned char)((uint32_t)v);
}

// CICS RESP values: only NORMAL (0) is ever compared against by name in
// GenApp's VSAM programs (confirmed - every check is `= DFHRESP(NORMAL)` or
// `NOT = DFHRESP(NORMAL)`, never a specific failure code), so exact values
// beyond 0 don't need to match real CICS, only "nonzero".
constexpr int32_t RESP_NORMAL = 0;
constexpr int32_t RESP_NOTFND = 13;
constexpr int32_t RESP_DUPREC = 15;
constexpr int32_t RESP_INVREQ = 16;  // unknown file name, etc.

void set_resp(void* resp_v, int32_t value) {
    encode_comp32_be(reinterpret_cast<unsigned char*>(resp_v), value);
}

std::string fixed(const char* buf, int len) { return std::string(buf, len); }

}  // namespace

extern "C" {

// cobol: CALL "VSAMREAD" USING file-name(8) into-buf BY VALUE into-len
//        key-buf BY VALUE key-len resp BY VALUE gteq BY VALUE generic
void VSAMREAD(char* file_name_buf, char* into_buf, int into_len, char* key_buf,
              int key_len, void* resp_v, int gteq, int generic) {
    std::string file_name = fixed(file_name_buf, 8);
    size_t sp = file_name.find(' ');
    if (sp != std::string::npos) file_name.resize(sp);

    auto spec_it = kFiles.find(file_name);
    if (spec_it == kFiles.end()) {
        set_resp(resp_v, RESP_INVREQ);
        return;
    }
    load_if_needed(file_name, spec_it->second);
    auto& recs = g_state[file_name].records;
    std::string key = fixed(key_buf, key_len);

    std::map<std::string, std::string>::iterator it;
    if (gteq || generic) {
        it = recs.lower_bound(key);
        if (generic && it != recs.end() && it->first.compare(0, key.size(), key) != 0) {
            it = recs.end();  // lower_bound found something, but it doesn't share the prefix
        }
    } else {
        it = recs.find(key);
    }

    if (it == recs.end()) {
        set_resp(resp_v, RESP_NOTFND);
        return;
    }
    int copy_len = into_len < (int)it->second.size() ? into_len : (int)it->second.size();
    memcpy(into_buf, it->second.data(), copy_len);
    g_state[file_name].last_read_key = it->first;
    set_resp(resp_v, RESP_NORMAL);
}

// cobol: CALL "VSAMWRITE" USING file-name(8) from-buf BY VALUE from-len
//        key-buf BY VALUE key-len resp
void VSAMWRITE(char* file_name_buf, char* from_buf, int from_len, char* key_buf,
               int key_len, void* resp_v) {
    std::string file_name = fixed(file_name_buf, 8);
    size_t sp = file_name.find(' ');
    if (sp != std::string::npos) file_name.resize(sp);

    auto spec_it = kFiles.find(file_name);
    if (spec_it == kFiles.end()) {
        set_resp(resp_v, RESP_INVREQ);
        return;
    }
    load_if_needed(file_name, spec_it->second);
    std::string key = fixed(key_buf, key_len);
    auto& recs = g_state[file_name].records;
    if (recs.count(key)) {
        set_resp(resp_v, RESP_DUPREC);
        return;
    }
    recs[key] = fixed(from_buf, from_len);
    persist(file_name);
    set_resp(resp_v, RESP_NORMAL);
}

// cobol: CALL "VSAMREWRITE" USING file-name(8) from-buf BY VALUE from-len resp
// No RIDFLD - like real CICS REWRITE, updates the record from the last
// successful READ on this file.
void VSAMREWRITE(char* file_name_buf, char* from_buf, int from_len, void* resp_v) {
    std::string file_name = fixed(file_name_buf, 8);
    size_t sp = file_name.find(' ');
    if (sp != std::string::npos) file_name.resize(sp);

    auto spec_it = kFiles.find(file_name);
    if (spec_it == kFiles.end()) {
        set_resp(resp_v, RESP_INVREQ);
        return;
    }
    load_if_needed(file_name, spec_it->second);
    auto& st = g_state[file_name];
    if (st.last_read_key.empty() || !st.records.count(st.last_read_key)) {
        set_resp(resp_v, RESP_NOTFND);
        return;
    }
    st.records[st.last_read_key] = fixed(from_buf, from_len);
    persist(file_name);
    set_resp(resp_v, RESP_NORMAL);
}

// cobol: CALL "VSAMDELETE" USING file-name(8) key-buf BY VALUE key-len resp
void VSAMDELETE(char* file_name_buf, char* key_buf, int key_len, void* resp_v) {
    std::string file_name = fixed(file_name_buf, 8);
    size_t sp = file_name.find(' ');
    if (sp != std::string::npos) file_name.resize(sp);

    auto spec_it = kFiles.find(file_name);
    if (spec_it == kFiles.end()) {
        set_resp(resp_v, RESP_INVREQ);
        return;
    }
    load_if_needed(file_name, spec_it->second);
    std::string key = fixed(key_buf, key_len);
    auto& recs = g_state[file_name].records;
    if (!recs.erase(key)) {
        set_resp(resp_v, RESP_NOTFND);
        return;
    }
    persist(file_name);
    set_resp(resp_v, RESP_NORMAL);
}

}  // extern "C"
