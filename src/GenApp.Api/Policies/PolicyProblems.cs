using GenApp.Application;
using Microsoft.AspNetCore.Mvc;

namespace GenApp.Api.Policies;

// Error responses of the policy controllers; only the type name in the not-found message differs between them.
internal static class PolicyProblems
{
    // Add: the only failure is 70, no such customer (lgapdb01.cbl:295-298).
    public static IActionResult CustomerNotFound(string returnCode, int customerNumber) =>
        ReturnCodeProblems.For(returnCode, $"{customerNumber} numaralı müşteri bulunamadı.").ToResult();

    // Inquire, update and delete. typeName is the policy type as written in the message, e.g. "motor poliçesi".
    public static IActionResult For(string returnCode, int customerNumber, int policyNumber, string typeName) =>
        ReturnCodeProblems.For(returnCode, returnCode switch
        {
            ReturnCodes.ConcurrencyConflict =>
                $"{policyNumber} numaralı poliçe siz okuduktan sonra değiştirilmiş. Poliçeyi yeniden sorgulayıp tekrar deneyin.",
            ReturnCodes.DatabaseError =>
                $"{customerNumber} numaralı müşterinin {policyNumber} numaralı poliçesi silinemedi.",
            _ => $"{customerNumber} numaralı müşterinin {policyNumber} numaralı {typeName} bulunamadı.",
        }).ToResult();
}
