# Yakalanmayan mutantlar

Stryker.NET'in yakalayamadığı (`Survived`: mutant etkinken testler yine geçti) ya da hiçbir testin çalıştırmadığı (`NoCoverage`) mutantların tam listesi. Özet ve yorum: [`mutation.md`](mutation.md).

## Yalnız eşdeğerlik testleri (295)

| Yer | Durum | Mutasyon | Satır | Yerine |
|---|---|---|---|---|
| src/GenApp.Domain/Legacy/ClaimCommarea.cs:7 | Survived | String mutation | `public string ClaimDate { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/ClaimCommarea.cs:10 | Survived | String mutation | `public string Cause { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/ClaimCommarea.cs:11 | Survived | String mutation | `public string Observations { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/ClaimCommarea.cs:15 | Survived | Statement mutation | `writer.Number(ClaimNumber, 10);   // CA-C-Num          9(10)` | `;` |
| src/GenApp.Domain/Legacy/ClaimCommarea.cs:16 | Survived | Statement mutation | `writer.Text(ClaimDate, 10);       // CA-C-Date         X(10)` | `;` |
| src/GenApp.Domain/Legacy/ClaimCommarea.cs:17 | Survived | Statement mutation | `writer.Number(Paid, 8);           // CA-C-Paid         9(8)` | `;` |
| src/GenApp.Domain/Legacy/ClaimCommarea.cs:18 | Survived | Statement mutation | `writer.Number(Value, 8);          // CA-C-Value        9(8)` | `;` |
| src/GenApp.Domain/Legacy/ClaimCommarea.cs:19 | Survived | Statement mutation | `writer.Text(Cause, 255);          // CA-C-Cause        X(255)` | `;` |
| src/GenApp.Domain/Legacy/ClaimCommarea.cs:20 | Survived | Statement mutation | `writer.Text(Observations, 255);   // CA-C-Observations X(255)` | `;` |
| src/GenApp.Domain/Legacy/ClaimCommarea.cs:24 | Survived | Block removal mutation | `{` | `{}` |
| src/GenApp.Domain/Legacy/Commarea.cs:9 | Survived | String mutation | `public string RequestId { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/Commarea.cs:16 | Survived | Statement mutation | `writer.Text(RequestId, 6);        // CA-REQUEST-ID    X(6)` | `;` |
| src/GenApp.Domain/Legacy/Commarea.cs:17 | Survived | Statement mutation | `writer.Number(ReturnCode, 2);     // CA-RETURN-CODE   9(2)` | `;` |
| src/GenApp.Domain/Legacy/Commarea.cs:18 | Survived | Statement mutation | `writer.Number(CustomerNumber, 10); // CA-CUSTOMER-NUM 9(10)` | `;` |
| src/GenApp.Domain/Legacy/Commarea.cs:19 | Survived | Statement mutation | `WriteRequestSpecific(writer);` | `;` |
| src/GenApp.Domain/Legacy/Commarea.cs:28 | Survived | Equality mutation | `if (raw.Length != Length)` | `raw.Length == Length` |
| src/GenApp.Domain/Legacy/Commarea.cs:30 | Survived | Statement mutation | `throw new ArgumentException($"A COMMAREA is {Length} characters long, but {raw.Length} wer` | `;` |
| src/GenApp.Domain/Legacy/Commarea.cs:30 | Survived | String mutation | `throw new ArgumentException($"A COMMAREA is {Length} characters long, but {raw.Length} wer` | `$""` |
| src/GenApp.Domain/Legacy/Commarea.cs:34 | Survived | Object initializer mutation | `var commarea = new T` | `new T {}` |
| src/GenApp.Domain/Legacy/Commarea.cs:40 | Survived | Statement mutation | `commarea.ReadRequestSpecific(reader);` | `;` |
| src/GenApp.Domain/Legacy/CommareaReader.cs:17 | Survived | LogicalNotExpression to un-LogicalNotExpression mutation | `if (!field.All(char.IsAsciiDigit))` | `field.All(char.IsAsciiDigit)` |
| src/GenApp.Domain/Legacy/CommareaReader.cs:17 | Survived | Linq method mutation (All() to Any()) | `if (!field.All(char.IsAsciiDigit))` | `field.Any` |
| src/GenApp.Domain/Legacy/CommareaReader.cs:19 | Survived | Statement mutation | `throw new FormatException($"Numeric field at position {_position - length} is not all digi` | `;` |
| src/GenApp.Domain/Legacy/CommareaReader.cs:19 | Survived | String mutation | `throw new FormatException($"Numeric field at position {_position - length} is not all digi` | `$""` |
| src/GenApp.Domain/Legacy/CommareaReader.cs:19 | Survived | Arithmetic mutation | `throw new FormatException($"Numeric field at position {_position - length} is not all digi` | `_position + length` |
| src/GenApp.Domain/Legacy/CommareaReader.cs:28 | Survived | AddAssignmentExpression to SubtractAssignmentExpression mutation | `_position += length;` | `_position -= length` |
| src/GenApp.Domain/Legacy/CommareaWriter.cs:12 | Survived | Null coalescing mutation (remove left) | `var text = value ?? "";` | `""` |
| src/GenApp.Domain/Legacy/CommareaWriter.cs:12 | Survived | String mutation | `var text = value ?? "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CommareaWriter.cs:13 | Survived | Statement mutation | `_buffer.Append(text.Length > length ? text[..length] : text.PadRight(length));` | `;` |
| src/GenApp.Domain/Legacy/CommareaWriter.cs:13 | Survived | Conditional (true) mutation | `_buffer.Append(text.Length > length ? text[..length] : text.PadRight(length));` | `(true?text[..length] :text.PadRight(length))` |
| src/GenApp.Domain/Legacy/CommareaWriter.cs:13 | Survived | Conditional (false) mutation | `_buffer.Append(text.Length > length ? text[..length] : text.PadRight(length));` | `(false?text[..length] :text.PadRight(length))` |
| src/GenApp.Domain/Legacy/CommareaWriter.cs:13 | Survived | Equality mutation | `_buffer.Append(text.Length > length ? text[..length] : text.PadRight(length));` | `text.Length < length` |
| src/GenApp.Domain/Legacy/CommareaWriter.cs:13 | Survived | Equality mutation | `_buffer.Append(text.Length > length ? text[..length] : text.PadRight(length));` | `text.Length >= length` |
| src/GenApp.Domain/Legacy/CommareaWriter.cs:20 | Survived | Statement mutation | `_buffer.Append(digits.Length > length ? digits[^length..] : digits.PadLeft(length, '0'));` | `;` |
| src/GenApp.Domain/Legacy/CommareaWriter.cs:20 | Survived | Conditional (true) mutation | `_buffer.Append(digits.Length > length ? digits[^length..] : digits.PadLeft(length, '0'));` | `(true?digits[^length..] :digits.PadLeft(length, '0'))` |
| src/GenApp.Domain/Legacy/CommareaWriter.cs:20 | Survived | Conditional (false) mutation | `_buffer.Append(digits.Length > length ? digits[^length..] : digits.PadLeft(length, '0'));` | `(false?digits[^length..] :digits.PadLeft(length, '0'))` |
| src/GenApp.Domain/Legacy/CommareaWriter.cs:20 | Survived | Equality mutation | `_buffer.Append(digits.Length > length ? digits[^length..] : digits.PadLeft(length, '0'));` | `digits.Length < length` |
| src/GenApp.Domain/Legacy/CommareaWriter.cs:20 | Survived | Equality mutation | `_buffer.Append(digits.Length > length ? digits[^length..] : digits.PadLeft(length, '0'));` | `digits.Length >= length` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:6 | Survived | String mutation | `public string Address { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:7 | Survived | String mutation | `public string Postcode { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:8 | Survived | String mutation | `public string Latitude { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:9 | Survived | String mutation | `public string Longitude { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:10 | Survived | String mutation | `public string Customer { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:11 | Survived | String mutation | `public string PropertyType { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:21 | Survived | String mutation | `public string RejectionReason { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:25 | Survived | Statement mutation | `writer.Text(Address, 255);        // CA-B-Address        X(255)` | `;` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:26 | Survived | Statement mutation | `writer.Text(Postcode, 8);         // CA-B-Postcode       X(8)` | `;` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:27 | Survived | Statement mutation | `writer.Text(Latitude, 11);        // CA-B-Latitude       X(11)` | `;` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:28 | Survived | Statement mutation | `writer.Text(Longitude, 11);       // CA-B-Longitude      X(11)` | `;` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:29 | Survived | Statement mutation | `writer.Text(Customer, 255);       // CA-B-Customer       X(255)` | `;` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:30 | Survived | Statement mutation | `writer.Text(PropertyType, 255);   // CA-B-PropType       X(255)` | `;` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:31 | Survived | Statement mutation | `writer.Number(FirePeril, 4);      // CA-B-FirePeril      9(4)` | `;` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:32 | Survived | Statement mutation | `writer.Number(FirePremium, 8);    // CA-B-FirePremium    9(8)` | `;` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:33 | Survived | Statement mutation | `writer.Number(CrimePeril, 4);     // CA-B-CrimePeril     9(4)` | `;` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:34 | Survived | Statement mutation | `writer.Number(CrimePremium, 8);   // CA-B-CrimePremium   9(8)` | `;` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:35 | Survived | Statement mutation | `writer.Number(FloodPeril, 4);     // CA-B-FloodPeril     9(4)` | `;` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:36 | Survived | Statement mutation | `writer.Number(FloodPremium, 8);   // CA-B-FloodPremium   9(8)` | `;` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:37 | Survived | Statement mutation | `writer.Number(WeatherPeril, 4);   // CA-B-WeatherPeril   9(4)` | `;` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:38 | Survived | Statement mutation | `writer.Number(WeatherPremium, 8); // CA-B-WeatherPremium 9(8)` | `;` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:39 | Survived | Statement mutation | `writer.Number(Status, 4);         // CA-B-Status         9(4)` | `;` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:40 | Survived | Statement mutation | `writer.Text(RejectionReason, 255); // CA-B-RejectReason  X(255)` | `;` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:44 | Survived | Block removal mutation | `{` | `{}` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:6 | Survived | String mutation | `public string FirstName { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:7 | Survived | String mutation | `public string LastName { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:8 | Survived | String mutation | `public string DateOfBirth { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:9 | Survived | String mutation | `public string HouseName { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:10 | Survived | String mutation | `public string HouseNumber { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:11 | Survived | String mutation | `public string Postcode { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:13 | Survived | String mutation | `public string PhoneMobile { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:14 | Survived | String mutation | `public string PhoneHome { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:15 | Survived | String mutation | `public string EmailAddress { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:19 | Survived | Statement mutation | `writer.Text(FirstName, 10);       // CA-FIRST-NAME     X(10)` | `;` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:20 | Survived | Statement mutation | `writer.Text(LastName, 20);        // CA-LAST-NAME      X(20)` | `;` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:21 | Survived | Statement mutation | `writer.Text(DateOfBirth, 10);     // CA-DOB            X(10)` | `;` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:22 | Survived | Statement mutation | `writer.Text(HouseName, 20);       // CA-HOUSE-NAME     X(20)` | `;` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:23 | Survived | Statement mutation | `writer.Text(HouseNumber, 4);      // CA-HOUSE-NUM      X(4)` | `;` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:24 | Survived | Statement mutation | `writer.Text(Postcode, 8);         // CA-POSTCODE       X(8)` | `;` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:25 | Survived | Statement mutation | `writer.Number(NumberOfPolicies, 3); // CA-NUM-POLICIES 9(3)` | `;` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:26 | Survived | Statement mutation | `writer.Text(PhoneMobile, 20);     // CA-PHONE-MOBILE   X(20)` | `;` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:27 | Survived | Statement mutation | `writer.Text(PhoneHome, 20);       // CA-PHONE-HOME     X(20)` | `;` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:28 | Survived | Statement mutation | `writer.Text(EmailAddress, 100);   // CA-EMAIL-ADDRESS  X(100)` | `;` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:32 | Survived | Block removal mutation | `{` | `{}` |
| src/GenApp.Domain/Legacy/CustomerSecurityCommarea.cs:6 | Survived | String mutation | `public string Password { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CustomerSecurityCommarea.cs:7 | Survived | String mutation | `public string Count { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CustomerSecurityCommarea.cs:8 | Survived | String mutation | `public string State { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CustomerSecurityCommarea.cs:12 | Survived | Statement mutation | `writer.Text(Password, 32);        // CA-CUSTSECR-PASS  X(32)` | `;` |
| src/GenApp.Domain/Legacy/CustomerSecurityCommarea.cs:13 | Survived | Statement mutation | `writer.Text(Count, 4);            // CA-CUSTSECR-COUNT X(4)` | `;` |
| src/GenApp.Domain/Legacy/CustomerSecurityCommarea.cs:14 | Survived | Statement mutation | `writer.Text(State, 1);            // CA-CUSTSECR-STATE X(1)` | `;` |
| src/GenApp.Domain/Legacy/CustomerSecurityCommarea.cs:18 | Survived | Block removal mutation | `{` | `{}` |
| src/GenApp.Domain/Legacy/EndowmentPolicyCommarea.cs:7 | Survived | String mutation | `public string WithProfits { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/EndowmentPolicyCommarea.cs:8 | Survived | String mutation | `public string Equities { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/EndowmentPolicyCommarea.cs:9 | Survived | String mutation | `public string ManagedFund { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/EndowmentPolicyCommarea.cs:10 | Survived | String mutation | `public string FundName { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/EndowmentPolicyCommarea.cs:13 | Survived | String mutation | `public string LifeAssured { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/EndowmentPolicyCommarea.cs:14 | Survived | String mutation | `public string PaddingData { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/EndowmentPolicyCommarea.cs:18 | Survived | Statement mutation | `writer.Text(WithProfits, 1);      // CA-E-WITH-PROFITS X` | `;` |
| src/GenApp.Domain/Legacy/EndowmentPolicyCommarea.cs:19 | Survived | Statement mutation | `writer.Text(Equities, 1);         // CA-E-EQUITIES     X` | `;` |
| src/GenApp.Domain/Legacy/EndowmentPolicyCommarea.cs:20 | Survived | Statement mutation | `writer.Text(ManagedFund, 1);      // CA-E-MANAGED-FUND X` | `;` |
| src/GenApp.Domain/Legacy/EndowmentPolicyCommarea.cs:21 | Survived | Statement mutation | `writer.Text(FundName, 10);        // CA-E-FUND-NAME    X(10)` | `;` |
| src/GenApp.Domain/Legacy/EndowmentPolicyCommarea.cs:22 | Survived | Statement mutation | `writer.Number(Term, 2);           // CA-E-TERM         99` | `;` |
| src/GenApp.Domain/Legacy/EndowmentPolicyCommarea.cs:23 | Survived | Statement mutation | `writer.Number(SumAssured, 6);     // CA-E-SUM-ASSURED  9(6)` | `;` |
| src/GenApp.Domain/Legacy/EndowmentPolicyCommarea.cs:24 | Survived | Statement mutation | `writer.Text(LifeAssured, 31);     // CA-E-LIFE-ASSURED X(31)` | `;` |
| src/GenApp.Domain/Legacy/EndowmentPolicyCommarea.cs:25 | Survived | Statement mutation | `writer.Text(PaddingData, 32348); // CA-E-PADDING-DATA X(32348)` | `;` |
| src/GenApp.Domain/Legacy/EndowmentPolicyCommarea.cs:29 | Survived | Block removal mutation | `{` | `{}` |
| src/GenApp.Domain/Legacy/HousePolicyCommarea.cs:6 | Survived | String mutation | `public string PropertyType { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/HousePolicyCommarea.cs:9 | Survived | String mutation | `public string HouseName { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/HousePolicyCommarea.cs:10 | Survived | String mutation | `public string HouseNumber { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/HousePolicyCommarea.cs:11 | Survived | String mutation | `public string Postcode { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/HousePolicyCommarea.cs:15 | Survived | Statement mutation | `writer.Text(PropertyType, 15);    // CA-H-PROPERTY-TYPE X(15)` | `;` |
| src/GenApp.Domain/Legacy/HousePolicyCommarea.cs:16 | Survived | Statement mutation | `writer.Number(Bedrooms, 3);       // CA-H-BEDROOMS      9(3)` | `;` |
| src/GenApp.Domain/Legacy/HousePolicyCommarea.cs:17 | Survived | Statement mutation | `writer.Number(Value, 8);          // CA-H-VALUE         9(8)` | `;` |
| src/GenApp.Domain/Legacy/HousePolicyCommarea.cs:18 | Survived | Statement mutation | `writer.Text(HouseName, 20);       // CA-H-HOUSE-NAME    X(20)` | `;` |
| src/GenApp.Domain/Legacy/HousePolicyCommarea.cs:19 | Survived | Statement mutation | `writer.Text(HouseNumber, 4);      // CA-H-HOUSE-NUMBER  X(4)` | `;` |
| src/GenApp.Domain/Legacy/HousePolicyCommarea.cs:20 | Survived | Statement mutation | `writer.Text(Postcode, 8);         // CA-H-POSTCODE      X(8)` | `;` |
| src/GenApp.Domain/Legacy/HousePolicyCommarea.cs:24 | Survived | Block removal mutation | `{` | `{}` |
| src/GenApp.Domain/Legacy/MotorPolicyCommarea.cs:6 | Survived | String mutation | `public string Make { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/MotorPolicyCommarea.cs:7 | Survived | String mutation | `public string Model { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/MotorPolicyCommarea.cs:9 | Survived | String mutation | `public string RegistrationNumber { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/MotorPolicyCommarea.cs:10 | Survived | String mutation | `public string Colour { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/MotorPolicyCommarea.cs:12 | Survived | String mutation | `public string ManufactureDate { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/MotorPolicyCommarea.cs:18 | Survived | Statement mutation | `writer.Text(Make, 15);            // CA-M-MAKE         X(15)` | `;` |
| src/GenApp.Domain/Legacy/MotorPolicyCommarea.cs:19 | Survived | Statement mutation | `writer.Text(Model, 15);           // CA-M-MODEL        X(15)` | `;` |
| src/GenApp.Domain/Legacy/MotorPolicyCommarea.cs:20 | Survived | Statement mutation | `writer.Number(Value, 6);          // CA-M-VALUE        9(6)` | `;` |
| src/GenApp.Domain/Legacy/MotorPolicyCommarea.cs:21 | Survived | Statement mutation | `writer.Text(RegistrationNumber, 7); // CA-M-REGNUMBER  X(7)` | `;` |
| src/GenApp.Domain/Legacy/MotorPolicyCommarea.cs:22 | Survived | Statement mutation | `writer.Text(Colour, 8);           // CA-M-COLOUR       X(8)` | `;` |
| src/GenApp.Domain/Legacy/MotorPolicyCommarea.cs:23 | Survived | Statement mutation | `writer.Number(Cc, 4);             // CA-M-CC           9(4)` | `;` |
| src/GenApp.Domain/Legacy/MotorPolicyCommarea.cs:24 | Survived | Statement mutation | `writer.Text(ManufactureDate, 10); // CA-M-MANUFACTURED X(10)` | `;` |
| src/GenApp.Domain/Legacy/MotorPolicyCommarea.cs:25 | Survived | Statement mutation | `writer.Number(Premium, 6);        // CA-M-PREMIUM      9(6)` | `;` |
| src/GenApp.Domain/Legacy/MotorPolicyCommarea.cs:26 | Survived | Statement mutation | `writer.Number(Accidents, 6);      // CA-M-ACCIDENTS    9(6)` | `;` |
| src/GenApp.Domain/Legacy/MotorPolicyCommarea.cs:30 | Survived | Block removal mutation | `{` | `{}` |
| src/GenApp.Domain/Legacy/PolicyCommarea.cs:8 | Survived | String mutation | `public string IssueDate { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/PolicyCommarea.cs:9 | Survived | String mutation | `public string ExpiryDate { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/PolicyCommarea.cs:10 | Survived | String mutation | `public string LastChanged { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/PolicyCommarea.cs:12 | Survived | String mutation | `public string BrokersReference { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/PolicyCommarea.cs:17 | Survived | Statement mutation | `writer.Number(PolicyNumber, 10);  // CA-POLICY-NUM     9(10)` | `;` |
| src/GenApp.Domain/Legacy/PolicyCommarea.cs:18 | Survived | Statement mutation | `writer.Text(IssueDate, 10);       // CA-ISSUE-DATE     X(10)` | `;` |
| src/GenApp.Domain/Legacy/PolicyCommarea.cs:19 | Survived | Statement mutation | `writer.Text(ExpiryDate, 10);      // CA-EXPIRY-DATE    X(10)` | `;` |
| src/GenApp.Domain/Legacy/PolicyCommarea.cs:20 | Survived | Statement mutation | `writer.Text(LastChanged, 26);     // CA-LASTCHANGED    X(26)` | `;` |
| src/GenApp.Domain/Legacy/PolicyCommarea.cs:21 | Survived | Statement mutation | `writer.Number(BrokerId, 10);      // CA-BROKERID       9(10)` | `;` |
| src/GenApp.Domain/Legacy/PolicyCommarea.cs:22 | Survived | Statement mutation | `writer.Text(BrokersReference, 10); // CA-BROKERSREF    X(10)` | `;` |
| src/GenApp.Domain/Legacy/PolicyCommarea.cs:23 | Survived | Statement mutation | `writer.Number(Payment, 6);        // CA-PAYMENT        9(6)` | `;` |
| src/GenApp.Domain/Legacy/PolicyCommarea.cs:24 | Survived | Statement mutation | `WritePolicySpecific(writer);` | `;` |
| src/GenApp.Domain/Legacy/PolicyCommarea.cs:38 | Survived | Statement mutation | `ReadPolicySpecific(reader);` | `;` |
| src/GenApp.Application/Customers/CustomerService.cs:31 | Survived | String mutation | `StateIndicator = "N",` | `""` |
| src/GenApp.Application/Customers/CustomerService.cs:34 | Survived | Statement mutation | `await unitOfWork.SaveChangesAsync(cancellationToken);` | `;` |
| src/GenApp.Application/Customers/CustomerService.cs:56 | Survived | Conditional (false) mutation | `return updated == 0` | `(false?OperationResult<Customer>.Failure(ReturnCodes.NotFoun` |
| src/GenApp.Application/Policies/CommercialPolicyService.cs:15 | NoCoverage | String mutation | `throw new NotSupportedException("The legacy system has no commercial policy update.");` | `""` |
| src/GenApp.Application/Policies/CommercialPolicyService.cs:22 | Survived | Conditional (false) mutation | `return record is null \|\| record.Policy.PolicyType != PolicyType` | `(false?OperationResult<PolicyRecord<CommercialPolicy>>.Failu` |
| src/GenApp.Application/Policies/CommercialPolicyService.cs:22 | Survived | Logical mutation | `return record is null \|\| record.Policy.PolicyType != PolicyType` | `record is null && record.Policy.PolicyType != PolicyType` |
| src/GenApp.Application/Policies/PolicyService.cs:102 | NoCoverage | Block removal mutation | `{` | `{}` |
| src/GenApp.Application/Policies/PolicyService.cs:107 | Survived | Conditional (false) mutation | `return deleted == 0 ? ReturnCodes.DatabaseError : ReturnCodes.Success;` | `(false?ReturnCodes.DatabaseError :ReturnCodes.Success)` |
| src/GenApp.Application/Policies/PolicyService.cs:117 | Survived | Conditional (true) mutation | `return record?.Policy.PolicyType == PolicyType ? record : null;` | `(true?record :null)` |
| src/GenApp.Infrastructure/Persistence/Configurations/ClaimConfiguration.cs:12 | Survived | Statement mutation | `b.ToTable("claim");` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/ClaimConfiguration.cs:12 | Survived | String mutation | `b.ToTable("claim");` | `""` |
| src/GenApp.Infrastructure/Persistence/Configurations/ClaimConfiguration.cs:13 | Survived | Statement mutation | `b.HasKey(c => c.ClaimNumber);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/ClaimConfiguration.cs:15 | Survived | Statement mutation | `b.Property(c => c.ClaimNumber)` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/ClaimConfiguration.cs:19 | Survived | Statement mutation | `b.Property(c => c.Cause).HasMaxLength(255);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/ClaimConfiguration.cs:20 | Survived | Statement mutation | `b.Property(c => c.Observations).HasMaxLength(255);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/ClaimConfiguration.cs:22 | Survived | Statement mutation | `b.HasOne<Policy>()` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CommercialPolicyConfiguration.cs:11 | Survived | Statement mutation | `b.ToTable("commercial");` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CommercialPolicyConfiguration.cs:11 | Survived | String mutation | `b.ToTable("commercial");` | `""` |
| src/GenApp.Infrastructure/Persistence/Configurations/CommercialPolicyConfiguration.cs:12 | Survived | Statement mutation | `b.HasKey(c => c.PolicyNumber);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CommercialPolicyConfiguration.cs:13 | Survived | Statement mutation | `b.Property(c => c.PolicyNumber).ValueGeneratedNever();` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CommercialPolicyConfiguration.cs:15 | Survived | Statement mutation | `b.Property(c => c.RequestDate).HasColumnType("timestamp without time zone");` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CommercialPolicyConfiguration.cs:15 | Survived | String mutation | `b.Property(c => c.RequestDate).HasColumnType("timestamp without time zone");` | `""` |
| src/GenApp.Infrastructure/Persistence/Configurations/CommercialPolicyConfiguration.cs:16 | Survived | Statement mutation | `b.Property(c => c.Address).HasMaxLength(255);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CommercialPolicyConfiguration.cs:17 | Survived | Statement mutation | `b.Property(c => c.Zipcode).HasMaxLength(8);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CommercialPolicyConfiguration.cs:18 | Survived | Statement mutation | `b.Property(c => c.LatitudeN).HasMaxLength(11);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CommercialPolicyConfiguration.cs:19 | Survived | Statement mutation | `b.Property(c => c.LongitudeW).HasMaxLength(11);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CommercialPolicyConfiguration.cs:20 | Survived | Statement mutation | `b.Property(c => c.Customer).HasMaxLength(255);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CommercialPolicyConfiguration.cs:21 | Survived | Statement mutation | `b.Property(c => c.PropertyType).HasMaxLength(255);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CommercialPolicyConfiguration.cs:22 | Survived | Statement mutation | `b.Property(c => c.RejectionReason).HasMaxLength(255);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CommercialPolicyConfiguration.cs:24 | Survived | Statement mutation | `b.HasOne(c => c.Policy)` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs:14 | Survived | Statement mutation | `b.Property(c => c.CustomerNumber)` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs:18 | Survived | Statement mutation | `b.Property(c => c.FirstName).HasMaxLength(10);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs:19 | Survived | Statement mutation | `b.Property(c => c.LastName).HasMaxLength(20);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs:20 | Survived | Statement mutation | `b.Property(c => c.HouseName).HasMaxLength(20);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs:21 | Survived | Statement mutation | `b.Property(c => c.HouseNumber).HasMaxLength(4);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs:22 | Survived | Statement mutation | `b.Property(c => c.Postcode).HasMaxLength(8);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs:23 | Survived | Statement mutation | `b.Property(c => c.PhoneHome).HasMaxLength(20);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs:24 | Survived | Statement mutation | `b.Property(c => c.PhoneMobile).HasMaxLength(20);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs:25 | Survived | Statement mutation | `b.Property(c => c.EmailAddress).HasMaxLength(100);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerSecureConfiguration.cs:11 | Survived | Statement mutation | `b.ToTable("customer_secure");` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerSecureConfiguration.cs:13 | Survived | Statement mutation | `b.Property(s => s.CustomerNumber).ValueGeneratedNever();` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerSecureConfiguration.cs:15 | Survived | Statement mutation | `b.Property(s => s.CustomerPass).HasMaxLength(32);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerSecureConfiguration.cs:16 | Survived | Statement mutation | `b.Property(s => s.StateIndicator).HasMaxLength(1);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerSecureConfiguration.cs:18 | Survived | Statement mutation | `b.HasOne<Customer>()` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/EndowmentPolicyConfiguration.cs:11 | Survived | Statement mutation | `b.ToTable("endowment");` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/EndowmentPolicyConfiguration.cs:13 | Survived | Statement mutation | `b.Property(e => e.PolicyNumber).ValueGeneratedNever();` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/EndowmentPolicyConfiguration.cs:15 | Survived | Statement mutation | `b.Property(e => e.Equities).HasMaxLength(1);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/EndowmentPolicyConfiguration.cs:16 | Survived | Statement mutation | `b.Property(e => e.WithProfits).HasMaxLength(1);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/EndowmentPolicyConfiguration.cs:17 | Survived | Statement mutation | `b.Property(e => e.ManagedFund).HasMaxLength(1);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/EndowmentPolicyConfiguration.cs:18 | Survived | Statement mutation | `b.Property(e => e.FundName).HasMaxLength(10);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/EndowmentPolicyConfiguration.cs:19 | Survived | Statement mutation | `b.Property(e => e.LifeAssured).HasMaxLength(31);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/EndowmentPolicyConfiguration.cs:20 | Survived | Statement mutation | `b.Property(e => e.PaddingData).HasMaxLength(32606);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/EndowmentPolicyConfiguration.cs:22 | Survived | Statement mutation | `b.HasOne(e => e.Policy)` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/HousePolicyConfiguration.cs:11 | Survived | Statement mutation | `b.ToTable("house");` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/HousePolicyConfiguration.cs:13 | Survived | Statement mutation | `b.Property(h => h.PolicyNumber).ValueGeneratedNever();` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/HousePolicyConfiguration.cs:15 | Survived | Statement mutation | `b.Property(h => h.PropertyType).HasMaxLength(15);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/HousePolicyConfiguration.cs:16 | Survived | Statement mutation | `b.Property(h => h.HouseName).HasMaxLength(20);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/HousePolicyConfiguration.cs:17 | Survived | Statement mutation | `b.Property(h => h.HouseNumber).HasMaxLength(4);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/HousePolicyConfiguration.cs:18 | Survived | Statement mutation | `b.Property(h => h.Postcode).HasMaxLength(8);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/HousePolicyConfiguration.cs:20 | Survived | Statement mutation | `b.HasOne(h => h.Policy)` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/MotorPolicyConfiguration.cs:11 | Survived | Statement mutation | `b.ToTable("motor");` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/MotorPolicyConfiguration.cs:13 | Survived | Statement mutation | `b.Property(m => m.PolicyNumber).ValueGeneratedNever();` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/MotorPolicyConfiguration.cs:15 | Survived | Statement mutation | `b.Property(m => m.Make).HasMaxLength(15);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/MotorPolicyConfiguration.cs:16 | Survived | Statement mutation | `b.Property(m => m.Model).HasMaxLength(15);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/MotorPolicyConfiguration.cs:17 | Survived | Statement mutation | `b.Property(m => m.RegNumber).HasMaxLength(7);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/MotorPolicyConfiguration.cs:18 | Survived | Statement mutation | `b.Property(m => m.Colour).HasMaxLength(8);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/MotorPolicyConfiguration.cs:20 | Survived | Statement mutation | `b.HasOne(m => m.Policy)` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/PolicyConfiguration.cs:12 | Survived | Statement mutation | `b.ToTable("policy");` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/PolicyConfiguration.cs:15 | Survived | Statement mutation | `b.Property(p => p.PolicyNumber)` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/PolicyConfiguration.cs:19 | Survived | Statement mutation | `b.Property(p => p.PolicyType).HasMaxLength(1);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/PolicyConfiguration.cs:20 | Survived | Statement mutation | `b.Property(p => p.BrokersReference).HasMaxLength(10);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/PolicyConfiguration.cs:22 | Survived | Statement mutation | `b.Property(p => p.LastChanged)` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/PolicyConfiguration.cs:27 | Survived | Statement mutation | `b.HasOne<Customer>()` | `;` |
| src/GenApp.Infrastructure/Persistence/DatabaseErrors.cs:20 | NoCoverage | Statement mutation | `throw Translate(e);` | `;` |
| src/GenApp.Infrastructure/Persistence/DatabaseErrors.cs:32 | NoCoverage | Statement mutation | `throw Translate(e);` | `;` |
| src/GenApp.Infrastructure/Persistence/DatabaseErrors.cs:38 | NoCoverage | Linq method mutation (Any() to All()) | `public static bool IsDatabaseError(Exception e) => Chain(e).Any(c => c is DbException or D` | `Chain(e).All` |
| src/GenApp.Infrastructure/Persistence/DatabaseErrors.cs:38 | NoCoverage | Equality mutation | `public static bool IsDatabaseError(Exception e) => Chain(e).Any(c => c is DbException or D` | `c is not DbException or DbUpdateException` |
| src/GenApp.Infrastructure/Persistence/DatabaseErrors.cs:41 | NoCoverage | Conditional (true) mutation | `IsBusy(e)` | `(true?new DatabaseBusyException("The database is busy (deadl` |
| src/GenApp.Infrastructure/Persistence/DatabaseErrors.cs:41 | NoCoverage | Conditional (false) mutation | `IsBusy(e)` | `(false?new DatabaseBusyException("The database is busy (dead` |
| src/GenApp.Infrastructure/Persistence/DatabaseErrors.cs:42 | NoCoverage | String mutation | `? new DatabaseBusyException("The database is busy (deadlock or timeout).", e)` | `""` |
| src/GenApp.Infrastructure/Persistence/DatabaseErrors.cs:43 | NoCoverage | String mutation | `: new DatabaseException("Database error.", e);` | `""` |
| src/GenApp.Infrastructure/Persistence/DatabaseErrors.cs:48 | NoCoverage | Linq method mutation (Any() to All()) | `Chain(e).Any(c => c` | `Chain(e).All` |
| src/GenApp.Infrastructure/Persistence/DatabaseErrors.cs:48 | NoCoverage | Equality mutation | `Chain(e).Any(c => c` | `c             is not PostgresException { SqlState: PostgresE` |
| src/GenApp.Infrastructure/Persistence/DatabaseErrors.cs:49 | NoCoverage | Logical mutation | `is PostgresException { SqlState: PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes` | `PostgresException { SqlState: PostgresErrorCodes.DeadlockDet` |
| src/GenApp.Infrastructure/Persistence/DatabaseErrors.cs:49 | NoCoverage | Logical mutation | `is PostgresException { SqlState: PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes` | `PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.Lo` |
| src/GenApp.Infrastructure/Persistence/DatabaseErrors.cs:49 | NoCoverage | Logical mutation | `is PostgresException { SqlState: PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes` | `PostgresErrorCodes.DeadlockDetected and PostgresErrorCodes.L` |
| src/GenApp.Infrastructure/Persistence/DatabaseErrors.cs:54 | NoCoverage | Equality mutation | `for (var current = e; current is not null; current = current.InnerException)` | `current is null` |
| src/GenApp.Infrastructure/Persistence/DatabaseErrors.cs:56 | NoCoverage | Statement mutation | `yield return current;` | `;` |
| src/GenApp.Infrastructure/Persistence/PolicyRepository.cs:18 | NoCoverage | String mutation | `_ => throw new ArgumentException($"Unknown policy type {details.GetType().Name}.", nameof(` | `$""` |
| src/GenApp.Infrastructure/Persistence/PolicyRepository.cs:56 | Survived | Linq method mutation (SingleOrDefault() to Single()) | `return list.SingleOrDefault();` | `list.Single` |
| src/GenApp.Infrastructure/Persistence/PolicyRepository.cs:83 | NoCoverage | Statement mutation | `throw DatabaseErrors.Translate(e);` | `;` |
| src/GenApp.Infrastructure/Persistence/PolicyRepository.cs:90 | Survived | Logical mutation | `.Where(p => p.CustomerNumber == customerNumber && p.PolicyNumber == policyNumber)` | `p.CustomerNumber == customerNumber \|\| p.PolicyNumber == po` |
| src/GenApp.Infrastructure/Persistence/PolicyRepository.cs:109 | Survived | Logical mutation | `.Where(p => p.CustomerNumber == customerNumber && p.PolicyNumber == policyNumber)` | `p.CustomerNumber == customerNumber \|\| p.PolicyNumber == po` |
| src/GenApp.Infrastructure/Persistence/PolicyRepository.cs:117 | Survived | Arithmetic mutation | `return new DateTime(now.Ticks - now.Ticks % 10, DateTimeKind.Unspecified);` | `now.Ticks + now.Ticks % 10` |
| src/GenApp.Infrastructure/Persistence/UnitOfWork.cs:19 | Survived | String mutation | `throw new ConcurrencyConflictException("The record was changed by someone else after it wa` | `""` |
| src/GenApp.Infrastructure/Persistence/UnitOfWork.cs:27 | NoCoverage | Statement mutation | `throw DatabaseErrors.Translate(e);` | `;` |
| src/GenApp.Api/CommercialPolicies/CommercialPoliciesController.cs:56 | Survived | Conditional (true) mutation | `return result.IsSuccess` | `(true?Ok(CommercialPolicyResponse.From(result.Value!)) :Retu` |
| src/GenApp.Api/CommercialPolicies/CommercialPoliciesController.cs:58 | NoCoverage | String mutation | `: ReturnCodeProblems.For(result.ReturnCode, $"{policyNumber} numaralı ticari poliçe buluna` | `$""` |
| src/GenApp.Api/CommercialPolicies/CommercialPolicyResponse.cs:15 | Survived | Null coalescing mutation (remove left) | `: base(policy, commercial.StartDate, commercial.RenewalDate, commercial.RequestDate ?? pol` | `policy.LastChanged` |
| src/GenApp.Api/Customers/CustomersController.cs:46 | Survived | Conditional (true) mutation | `return result.IsSuccess` | `(true?Ok(CustomerResponse.From(result.Value!)) :NotFoundProb` |
| src/GenApp.Api/Customers/CustomersController.cs:52 | Survived | String mutation | `ReturnCodeProblems.For(returnCode, $"{customerNumber} numaralı müşteri bulunamadı.").ToRes` | `$""` |
| src/GenApp.Api/DatabaseExceptionHandler.cs:12 | NoCoverage | Equality mutation | `if (exception is not DatabaseException)` | `exception is DatabaseException` |
| src/GenApp.Api/DatabaseExceptionHandler.cs:14 | NoCoverage | Boolean mutation | `return false;` | `true` |
| src/GenApp.Api/DatabaseExceptionHandler.cs:17 | NoCoverage | Statement mutation | `logger.LogError(exception, "Database failure");` | `;` |
| src/GenApp.Api/DatabaseExceptionHandler.cs:17 | NoCoverage | String mutation | `logger.LogError(exception, "Database failure");` | `""` |
| src/GenApp.Api/DatabaseExceptionHandler.cs:19 | NoCoverage | Conditional (true) mutation | `var problem = exception is DatabaseBusyException` | `(true?ReturnCodeProblems.DatabaseBusy() :ReturnCodeProblems.` |
| src/GenApp.Api/DatabaseExceptionHandler.cs:19 | NoCoverage | Conditional (false) mutation | `var problem = exception is DatabaseBusyException` | `(false?ReturnCodeProblems.DatabaseBusy() :ReturnCodeProblems` |
| src/GenApp.Api/DatabaseExceptionHandler.cs:24 | NoCoverage | Statement mutation | `await httpContext.Response.WriteAsJsonAsync(problem, options: null, contentType: "applicat` | `;` |
| src/GenApp.Api/DatabaseExceptionHandler.cs:24 | NoCoverage | String mutation | `await httpContext.Response.WriteAsJsonAsync(problem, options: null, contentType: "applicat` | `""` |
| src/GenApp.Api/DatabaseExceptionHandler.cs:25 | NoCoverage | Boolean mutation | `return true;` | `false` |
| src/GenApp.Api/MotorPolicies/MotorPoliciesController.cs:73 | Survived | Conditional (true) mutation | `return returnCode == ReturnCodes.Success` | `(true?NoContent() :PolicyProblems.For(returnCode, customerNu` |
| src/GenApp.Api/Policies/PolicyProblems.cs:11 | Survived | String mutation | `ReturnCodeProblems.For(returnCode, $"{customerNumber} numaralı müşteri bulunamadı.").ToRes` | `$""` |
| src/GenApp.Api/Policies/PolicyProblems.cs:18 | Survived | String mutation | `$"{policyNumber} numaralı poliçe siz okuduktan sonra değiştirilmiş. Poliçeyi yeniden sorgu` | `$""` |
| src/GenApp.Api/Policies/PolicyProblems.cs:20 | Survived | String mutation | `$"{customerNumber} numaralı müşterinin {policyNumber} numaralı poliçesi silinemedi.",` | `$""` |
| src/GenApp.Api/Policies/PolicyProblems.cs:21 | Survived | String mutation | `_ => $"{customerNumber} numaralı müşterinin {policyNumber} numaralı {typeName} bulunamadı.` | `$""` |
| src/GenApp.Api/Program.cs:9 | Survived | Null coalescing mutation (remove right) | `builder.Configuration.GetConnectionString("GenApp")` | `builder.Configuration.GetConnectionString("GenApp")` |
| src/GenApp.Api/Program.cs:10 | NoCoverage | String mutation | `?? throw new InvalidOperationException("Connection string 'GenApp' is not configured."));` | `""` |
| src/GenApp.Api/Program.cs:17 | Survived | String mutation | `options.ClientErrorMapping[StatusCodes.Status400BadRequest].Link = "validation-error";` | `""` |
| src/GenApp.Api/Program.cs:20 | Survived | Statement mutation | `builder.Services.AddExceptionHandler<DatabaseExceptionHandler>();` | `;` |
| src/GenApp.Api/Program.cs:25 | Survived | Statement mutation | `app.UseExceptionHandler();` | `;` |
| src/GenApp.Api/Program.cs:28 | Survived | Negate expression | `if (app.Environment.IsDevelopment())` | `!(app.Environment.IsDevelopment())` |
| src/GenApp.Api/Program.cs:30 | Survived | Statement mutation | `app.UseSwagger();` | `;` |
| src/GenApp.Api/Program.cs:31 | Survived | Statement mutation | `app.UseSwaggerUI();` | `;` |
| src/GenApp.Api/Program.cs:36 | Survived | Statement mutation | `app.UseDefaultFiles();` | `;` |
| src/GenApp.Api/Program.cs:37 | Survived | Statement mutation | `app.UseStaticFiles();` | `;` |
| src/GenApp.Api/ResponseText.cs:7 | NoCoverage | String mutation | `public static string Of(string? value) => value?.TrimEnd() ?? "";` | `"Stryker was here!"` |
| src/GenApp.Api/ReturnCodeProblems.cs:14 | Survived | String mutation | `[ReturnCodes.NotFound] = new(StatusCodes.Status404NotFound, "not-found", "Kayıt bulunamadı` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:15 | Survived | String mutation | `[ReturnCodes.ConcurrencyConflict] = new(StatusCodes.Status409Conflict, "concurrency-confli` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:16 | Survived | String mutation | `[ReturnCodes.CustomerNotFound] = new(StatusCodes.Status404NotFound, "customer-not-found", ` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:17 | Survived | String mutation | `["80"] = new(StatusCodes.Status500InternalServerError, "storage-error", "Depolama hatası")` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:17 | Survived | String mutation | `["80"] = new(StatusCodes.Status500InternalServerError, "storage-error", "Depolama hatası")` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:17 | Survived | String mutation | `["80"] = new(StatusCodes.Status500InternalServerError, "storage-error", "Depolama hatası")` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:18 | Survived | String mutation | `["81"] = new(StatusCodes.Status500InternalServerError, "storage-error", "Depolama hatası")` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:18 | Survived | String mutation | `["81"] = new(StatusCodes.Status500InternalServerError, "storage-error", "Depolama hatası")` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:18 | Survived | String mutation | `["81"] = new(StatusCodes.Status500InternalServerError, "storage-error", "Depolama hatası")` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:19 | Survived | String mutation | `["82"] = new(StatusCodes.Status500InternalServerError, "storage-error", "Depolama hatası")` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:19 | Survived | String mutation | `["82"] = new(StatusCodes.Status500InternalServerError, "storage-error", "Depolama hatası")` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:19 | Survived | String mutation | `["82"] = new(StatusCodes.Status500InternalServerError, "storage-error", "Depolama hatası")` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:20 | Survived | String mutation | `["88"] = new(StatusCodes.Status500InternalServerError, "database-error", "Veritabanı hatas` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:20 | Survived | String mutation | `["88"] = new(StatusCodes.Status500InternalServerError, "database-error", "Veritabanı hatas` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:20 | Survived | String mutation | `["88"] = new(StatusCodes.Status500InternalServerError, "database-error", "Veritabanı hatas` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:21 | Survived | String mutation | `["89"] = new(StatusCodes.Status500InternalServerError, "database-error", "Veritabanı hatas` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:21 | Survived | String mutation | `["89"] = new(StatusCodes.Status500InternalServerError, "database-error", "Veritabanı hatas` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:21 | Survived | String mutation | `["89"] = new(StatusCodes.Status500InternalServerError, "database-error", "Veritabanı hatas` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:22 | Survived | String mutation | `[ReturnCodes.DatabaseError] = new(StatusCodes.Status500InternalServerError, "database-erro` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:29 | NoCoverage | Statement mutation | `throw new ArgumentOutOfRangeException(nameof(returnCode), returnCode, "No HTTP mapping for` | `;` |
| src/GenApp.Api/ReturnCodeProblems.cs:29 | NoCoverage | String mutation | `throw new ArgumentOutOfRangeException(nameof(returnCode), returnCode, "No HTTP mapping for` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:37 | NoCoverage | String mutation | `Create(StatusCodes.Status503ServiceUnavailable, "database-busy", "Veritabanı meşgul", Retu` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:37 | NoCoverage | String mutation | `Create(StatusCodes.Status503ServiceUnavailable, "database-busy", "Veritabanı meşgul", Retu` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:38 | NoCoverage | String mutation | `"Veritabanı şu an meşgul, isteği daha sonra yeniden deneyin.");` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:41 | Survived | Object initializer mutation | `new ObjectResult(problem)` | `new ObjectResult(problem) {}` |
| src/GenApp.Api/ReturnCodeProblems.cs:44 | Survived | String mutation | `ContentTypes = { "application/problem+json" },` | `""` |

## Bütün testler (181)

| Yer | Durum | Mutasyon | Satır | Yerine |
|---|---|---|---|---|
| src/GenApp.Domain/Legacy/ClaimCommarea.cs:7 | Survived | String mutation | `public string ClaimDate { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/ClaimCommarea.cs:10 | Survived | String mutation | `public string Cause { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/ClaimCommarea.cs:11 | Survived | String mutation | `public string Observations { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/Commarea.cs:9 | Survived | String mutation | `public string RequestId { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/Commarea.cs:30 | Survived | String mutation | `throw new ArgumentException($"A COMMAREA is {Length} characters long, but {raw.Length} wer` | `$""` |
| src/GenApp.Domain/Legacy/CommareaReader.cs:19 | Survived | String mutation | `throw new FormatException($"Numeric field at position {_position - length} is not all digi` | `$""` |
| src/GenApp.Domain/Legacy/CommareaReader.cs:19 | Survived | Arithmetic mutation | `throw new FormatException($"Numeric field at position {_position - length} is not all digi` | `_position + length` |
| src/GenApp.Domain/Legacy/CommareaWriter.cs:13 | Survived | Equality mutation | `_buffer.Append(text.Length > length ? text[..length] : text.PadRight(length));` | `text.Length >= length` |
| src/GenApp.Domain/Legacy/CommareaWriter.cs:20 | Survived | Equality mutation | `_buffer.Append(digits.Length > length ? digits[^length..] : digits.PadLeft(length, '0'));` | `digits.Length >= length` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:6 | Survived | String mutation | `public string Address { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:7 | Survived | String mutation | `public string Postcode { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:8 | Survived | String mutation | `public string Latitude { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:9 | Survived | String mutation | `public string Longitude { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:10 | Survived | String mutation | `public string Customer { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:11 | Survived | String mutation | `public string PropertyType { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CommercialPolicyCommarea.cs:21 | Survived | String mutation | `public string RejectionReason { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:6 | Survived | String mutation | `public string FirstName { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:7 | Survived | String mutation | `public string LastName { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:8 | Survived | String mutation | `public string DateOfBirth { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:9 | Survived | String mutation | `public string HouseName { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:10 | Survived | String mutation | `public string HouseNumber { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:11 | Survived | String mutation | `public string Postcode { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:13 | Survived | String mutation | `public string PhoneMobile { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:14 | Survived | String mutation | `public string PhoneHome { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CustomerCommarea.cs:15 | Survived | String mutation | `public string EmailAddress { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CustomerSecurityCommarea.cs:6 | Survived | String mutation | `public string Password { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CustomerSecurityCommarea.cs:7 | Survived | String mutation | `public string Count { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/CustomerSecurityCommarea.cs:8 | Survived | String mutation | `public string State { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/EndowmentPolicyCommarea.cs:7 | Survived | String mutation | `public string WithProfits { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/EndowmentPolicyCommarea.cs:8 | Survived | String mutation | `public string Equities { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/EndowmentPolicyCommarea.cs:9 | Survived | String mutation | `public string ManagedFund { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/EndowmentPolicyCommarea.cs:10 | Survived | String mutation | `public string FundName { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/EndowmentPolicyCommarea.cs:13 | Survived | String mutation | `public string LifeAssured { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/EndowmentPolicyCommarea.cs:14 | Survived | String mutation | `public string PaddingData { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/HousePolicyCommarea.cs:6 | Survived | String mutation | `public string PropertyType { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/HousePolicyCommarea.cs:9 | Survived | String mutation | `public string HouseName { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/HousePolicyCommarea.cs:10 | Survived | String mutation | `public string HouseNumber { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/HousePolicyCommarea.cs:11 | Survived | String mutation | `public string Postcode { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/MotorPolicyCommarea.cs:6 | Survived | String mutation | `public string Make { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/MotorPolicyCommarea.cs:7 | Survived | String mutation | `public string Model { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/MotorPolicyCommarea.cs:9 | Survived | String mutation | `public string RegistrationNumber { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/MotorPolicyCommarea.cs:10 | Survived | String mutation | `public string Colour { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/MotorPolicyCommarea.cs:12 | Survived | String mutation | `public string ManufactureDate { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/PolicyCommarea.cs:8 | Survived | String mutation | `public string IssueDate { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/PolicyCommarea.cs:9 | Survived | String mutation | `public string ExpiryDate { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/PolicyCommarea.cs:10 | Survived | String mutation | `public string LastChanged { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Domain/Legacy/PolicyCommarea.cs:12 | Survived | String mutation | `public string BrokersReference { get; set; } = "";` | `"Stryker was here!"` |
| src/GenApp.Application/Customers/CustomerService.cs:31 | Survived | String mutation | `StateIndicator = "N",` | `""` |
| src/GenApp.Application/Policies/CommercialPolicyService.cs:15 | NoCoverage | String mutation | `throw new NotSupportedException("The legacy system has no commercial policy update.");` | `""` |
| src/GenApp.Application/Policies/CommercialPolicyService.cs:22 | Survived | Conditional (false) mutation | `return record is null \|\| record.Policy.PolicyType != PolicyType` | `(false?OperationResult<PolicyRecord<CommercialPolicy>>.Failu` |
| src/GenApp.Application/Policies/CommercialPolicyService.cs:22 | Survived | Logical mutation | `return record is null \|\| record.Policy.PolicyType != PolicyType` | `record is null && record.Policy.PolicyType != PolicyType` |
| src/GenApp.Application/Policies/PolicyService.cs:107 | Survived | Conditional (false) mutation | `return deleted == 0 ? ReturnCodes.DatabaseError : ReturnCodes.Success;` | `(false?ReturnCodes.DatabaseError :ReturnCodes.Success)` |
| src/GenApp.Application/Policies/PolicyService.cs:117 | Survived | Conditional (true) mutation | `return record?.Policy.PolicyType == PolicyType ? record : null;` | `(true?record :null)` |
| src/GenApp.Infrastructure/Persistence/Configurations/ClaimConfiguration.cs:12 | Survived | Statement mutation | `b.ToTable("claim");` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/ClaimConfiguration.cs:15 | Survived | Statement mutation | `b.Property(c => c.ClaimNumber)` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/ClaimConfiguration.cs:19 | Survived | Statement mutation | `b.Property(c => c.Cause).HasMaxLength(255);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/ClaimConfiguration.cs:20 | Survived | Statement mutation | `b.Property(c => c.Observations).HasMaxLength(255);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/ClaimConfiguration.cs:22 | Survived | Statement mutation | `b.HasOne<Policy>()` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CommercialPolicyConfiguration.cs:11 | Survived | Statement mutation | `b.ToTable("commercial");` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CommercialPolicyConfiguration.cs:13 | Survived | Statement mutation | `b.Property(c => c.PolicyNumber).ValueGeneratedNever();` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CommercialPolicyConfiguration.cs:15 | Survived | Statement mutation | `b.Property(c => c.RequestDate).HasColumnType("timestamp without time zone");` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CommercialPolicyConfiguration.cs:16 | Survived | Statement mutation | `b.Property(c => c.Address).HasMaxLength(255);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CommercialPolicyConfiguration.cs:17 | Survived | Statement mutation | `b.Property(c => c.Zipcode).HasMaxLength(8);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CommercialPolicyConfiguration.cs:18 | Survived | Statement mutation | `b.Property(c => c.LatitudeN).HasMaxLength(11);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CommercialPolicyConfiguration.cs:19 | Survived | Statement mutation | `b.Property(c => c.LongitudeW).HasMaxLength(11);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CommercialPolicyConfiguration.cs:20 | Survived | Statement mutation | `b.Property(c => c.Customer).HasMaxLength(255);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CommercialPolicyConfiguration.cs:21 | Survived | Statement mutation | `b.Property(c => c.PropertyType).HasMaxLength(255);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CommercialPolicyConfiguration.cs:22 | Survived | Statement mutation | `b.Property(c => c.RejectionReason).HasMaxLength(255);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CommercialPolicyConfiguration.cs:24 | Survived | Statement mutation | `b.HasOne(c => c.Policy)` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs:14 | Survived | Statement mutation | `b.Property(c => c.CustomerNumber)` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs:18 | Survived | Statement mutation | `b.Property(c => c.FirstName).HasMaxLength(10);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs:19 | Survived | Statement mutation | `b.Property(c => c.LastName).HasMaxLength(20);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs:20 | Survived | Statement mutation | `b.Property(c => c.HouseName).HasMaxLength(20);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs:21 | Survived | Statement mutation | `b.Property(c => c.HouseNumber).HasMaxLength(4);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs:22 | Survived | Statement mutation | `b.Property(c => c.Postcode).HasMaxLength(8);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs:23 | Survived | Statement mutation | `b.Property(c => c.PhoneHome).HasMaxLength(20);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs:24 | Survived | Statement mutation | `b.Property(c => c.PhoneMobile).HasMaxLength(20);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerConfiguration.cs:25 | Survived | Statement mutation | `b.Property(c => c.EmailAddress).HasMaxLength(100);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerSecureConfiguration.cs:11 | Survived | Statement mutation | `b.ToTable("customer_secure");` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerSecureConfiguration.cs:13 | Survived | Statement mutation | `b.Property(s => s.CustomerNumber).ValueGeneratedNever();` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerSecureConfiguration.cs:15 | Survived | Statement mutation | `b.Property(s => s.CustomerPass).HasMaxLength(32);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerSecureConfiguration.cs:16 | Survived | Statement mutation | `b.Property(s => s.StateIndicator).HasMaxLength(1);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/CustomerSecureConfiguration.cs:18 | Survived | Statement mutation | `b.HasOne<Customer>()` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/EndowmentPolicyConfiguration.cs:11 | Survived | Statement mutation | `b.ToTable("endowment");` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/EndowmentPolicyConfiguration.cs:13 | Survived | Statement mutation | `b.Property(e => e.PolicyNumber).ValueGeneratedNever();` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/EndowmentPolicyConfiguration.cs:15 | Survived | Statement mutation | `b.Property(e => e.Equities).HasMaxLength(1);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/EndowmentPolicyConfiguration.cs:16 | Survived | Statement mutation | `b.Property(e => e.WithProfits).HasMaxLength(1);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/EndowmentPolicyConfiguration.cs:17 | Survived | Statement mutation | `b.Property(e => e.ManagedFund).HasMaxLength(1);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/EndowmentPolicyConfiguration.cs:18 | Survived | Statement mutation | `b.Property(e => e.FundName).HasMaxLength(10);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/EndowmentPolicyConfiguration.cs:19 | Survived | Statement mutation | `b.Property(e => e.LifeAssured).HasMaxLength(31);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/EndowmentPolicyConfiguration.cs:20 | Survived | Statement mutation | `b.Property(e => e.PaddingData).HasMaxLength(32606);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/EndowmentPolicyConfiguration.cs:22 | Survived | Statement mutation | `b.HasOne(e => e.Policy)` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/HousePolicyConfiguration.cs:11 | Survived | Statement mutation | `b.ToTable("house");` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/HousePolicyConfiguration.cs:13 | Survived | Statement mutation | `b.Property(h => h.PolicyNumber).ValueGeneratedNever();` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/HousePolicyConfiguration.cs:15 | Survived | Statement mutation | `b.Property(h => h.PropertyType).HasMaxLength(15);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/HousePolicyConfiguration.cs:16 | Survived | Statement mutation | `b.Property(h => h.HouseName).HasMaxLength(20);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/HousePolicyConfiguration.cs:17 | Survived | Statement mutation | `b.Property(h => h.HouseNumber).HasMaxLength(4);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/HousePolicyConfiguration.cs:18 | Survived | Statement mutation | `b.Property(h => h.Postcode).HasMaxLength(8);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/HousePolicyConfiguration.cs:20 | Survived | Statement mutation | `b.HasOne(h => h.Policy)` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/MotorPolicyConfiguration.cs:11 | Survived | Statement mutation | `b.ToTable("motor");` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/MotorPolicyConfiguration.cs:13 | Survived | Statement mutation | `b.Property(m => m.PolicyNumber).ValueGeneratedNever();` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/MotorPolicyConfiguration.cs:15 | Survived | Statement mutation | `b.Property(m => m.Make).HasMaxLength(15);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/MotorPolicyConfiguration.cs:16 | Survived | Statement mutation | `b.Property(m => m.Model).HasMaxLength(15);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/MotorPolicyConfiguration.cs:17 | Survived | Statement mutation | `b.Property(m => m.RegNumber).HasMaxLength(7);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/MotorPolicyConfiguration.cs:18 | Survived | Statement mutation | `b.Property(m => m.Colour).HasMaxLength(8);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/MotorPolicyConfiguration.cs:20 | Survived | Statement mutation | `b.HasOne(m => m.Policy)` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/PolicyConfiguration.cs:12 | Survived | Statement mutation | `b.ToTable("policy");` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/PolicyConfiguration.cs:15 | Survived | Statement mutation | `b.Property(p => p.PolicyNumber)` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/PolicyConfiguration.cs:19 | Survived | Statement mutation | `b.Property(p => p.PolicyType).HasMaxLength(1);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/PolicyConfiguration.cs:20 | Survived | Statement mutation | `b.Property(p => p.BrokersReference).HasMaxLength(10);` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/PolicyConfiguration.cs:22 | Survived | Statement mutation | `b.Property(p => p.LastChanged)` | `;` |
| src/GenApp.Infrastructure/Persistence/Configurations/PolicyConfiguration.cs:27 | Survived | Statement mutation | `b.HasOne<Customer>()` | `;` |
| src/GenApp.Infrastructure/Persistence/DatabaseErrors.cs:32 | NoCoverage | Statement mutation | `throw Translate(e);` | `;` |
| src/GenApp.Infrastructure/Persistence/DatabaseErrors.cs:38 | Survived | Equality mutation | `public static bool IsDatabaseError(Exception e) => Chain(e).Any(c => c is DbException or D` | `c is not DbException or DbUpdateException` |
| src/GenApp.Infrastructure/Persistence/DatabaseErrors.cs:41 | Survived | Conditional (false) mutation | `IsBusy(e)` | `(false?new DatabaseBusyException("The database is busy (dead` |
| src/GenApp.Infrastructure/Persistence/DatabaseErrors.cs:42 | NoCoverage | String mutation | `? new DatabaseBusyException("The database is busy (deadlock or timeout).", e)` | `""` |
| src/GenApp.Infrastructure/Persistence/DatabaseErrors.cs:43 | Survived | String mutation | `: new DatabaseException("Database error.", e);` | `""` |
| src/GenApp.Infrastructure/Persistence/DatabaseErrors.cs:48 | Survived | Linq method mutation (Any() to All()) | `Chain(e).Any(c => c` | `Chain(e).All` |
| src/GenApp.Infrastructure/Persistence/DatabaseErrors.cs:49 | Survived | Logical mutation | `is PostgresException { SqlState: PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes` | `PostgresException { SqlState: PostgresErrorCodes.DeadlockDet` |
| src/GenApp.Infrastructure/Persistence/DatabaseErrors.cs:49 | Survived | Logical mutation | `is PostgresException { SqlState: PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes` | `PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes.Lo` |
| src/GenApp.Infrastructure/Persistence/DatabaseErrors.cs:49 | Survived | Logical mutation | `is PostgresException { SqlState: PostgresErrorCodes.DeadlockDetected or PostgresErrorCodes` | `PostgresErrorCodes.DeadlockDetected and PostgresErrorCodes.L` |
| src/GenApp.Infrastructure/Persistence/PolicyRepository.cs:18 | NoCoverage | String mutation | `_ => throw new ArgumentException($"Unknown policy type {details.GetType().Name}.", nameof(` | `$""` |
| src/GenApp.Infrastructure/Persistence/PolicyRepository.cs:56 | Survived | Linq method mutation (SingleOrDefault() to Single()) | `return list.SingleOrDefault();` | `list.Single` |
| src/GenApp.Infrastructure/Persistence/PolicyRepository.cs:83 | NoCoverage | Statement mutation | `throw DatabaseErrors.Translate(e);` | `;` |
| src/GenApp.Infrastructure/Persistence/UnitOfWork.cs:19 | Survived | String mutation | `throw new ConcurrencyConflictException("The record was changed by someone else after it wa` | `""` |
| src/GenApp.Infrastructure/Persistence/UnitOfWork.cs:27 | NoCoverage | Statement mutation | `throw DatabaseErrors.Translate(e);` | `;` |
| src/GenApp.Api/CommercialPolicies/CommercialPoliciesController.cs:56 | Survived | Conditional (true) mutation | `return result.IsSuccess` | `(true?Ok(CommercialPolicyResponse.From(result.Value!)) :Retu` |
| src/GenApp.Api/CommercialPolicies/CommercialPoliciesController.cs:58 | NoCoverage | String mutation | `: ReturnCodeProblems.For(result.ReturnCode, $"{policyNumber} numaralı ticari poliçe buluna` | `$""` |
| src/GenApp.Api/CommercialPolicies/CommercialPolicyResponse.cs:15 | Survived | Null coalescing mutation (remove left) | `: base(policy, commercial.StartDate, commercial.RenewalDate, commercial.RequestDate ?? pol` | `policy.LastChanged` |
| src/GenApp.Api/Customers/CustomersController.cs:52 | Survived | String mutation | `ReturnCodeProblems.For(returnCode, $"{customerNumber} numaralı müşteri bulunamadı.").ToRes` | `$""` |
| src/GenApp.Api/DatabaseExceptionHandler.cs:12 | NoCoverage | Equality mutation | `if (exception is not DatabaseException)` | `exception is DatabaseException` |
| src/GenApp.Api/DatabaseExceptionHandler.cs:14 | NoCoverage | Boolean mutation | `return false;` | `true` |
| src/GenApp.Api/DatabaseExceptionHandler.cs:17 | NoCoverage | Statement mutation | `logger.LogError(exception, "Database failure");` | `;` |
| src/GenApp.Api/DatabaseExceptionHandler.cs:17 | NoCoverage | String mutation | `logger.LogError(exception, "Database failure");` | `""` |
| src/GenApp.Api/DatabaseExceptionHandler.cs:19 | NoCoverage | Conditional (true) mutation | `var problem = exception is DatabaseBusyException` | `(true?ReturnCodeProblems.DatabaseBusy() :ReturnCodeProblems.` |
| src/GenApp.Api/DatabaseExceptionHandler.cs:19 | NoCoverage | Conditional (false) mutation | `var problem = exception is DatabaseBusyException` | `(false?ReturnCodeProblems.DatabaseBusy() :ReturnCodeProblems` |
| src/GenApp.Api/DatabaseExceptionHandler.cs:24 | NoCoverage | Statement mutation | `await httpContext.Response.WriteAsJsonAsync(problem, options: null, contentType: "applicat` | `;` |
| src/GenApp.Api/DatabaseExceptionHandler.cs:24 | NoCoverage | String mutation | `await httpContext.Response.WriteAsJsonAsync(problem, options: null, contentType: "applicat` | `""` |
| src/GenApp.Api/DatabaseExceptionHandler.cs:25 | NoCoverage | Boolean mutation | `return true;` | `false` |
| src/GenApp.Api/MotorPolicies/MotorPoliciesController.cs:73 | Survived | Conditional (true) mutation | `return returnCode == ReturnCodes.Success` | `(true?NoContent() :PolicyProblems.For(returnCode, customerNu` |
| src/GenApp.Api/Policies/PolicyProblems.cs:11 | Survived | String mutation | `ReturnCodeProblems.For(returnCode, $"{customerNumber} numaralı müşteri bulunamadı.").ToRes` | `$""` |
| src/GenApp.Api/Policies/PolicyProblems.cs:18 | Survived | String mutation | `$"{policyNumber} numaralı poliçe siz okuduktan sonra değiştirilmiş. Poliçeyi yeniden sorgu` | `$""` |
| src/GenApp.Api/Policies/PolicyProblems.cs:20 | Survived | String mutation | `$"{customerNumber} numaralı müşterinin {policyNumber} numaralı poliçesi silinemedi.",` | `$""` |
| src/GenApp.Api/Policies/PolicyProblems.cs:21 | Survived | String mutation | `_ => $"{customerNumber} numaralı müşterinin {policyNumber} numaralı {typeName} bulunamadı.` | `$""` |
| src/GenApp.Api/Program.cs:9 | Survived | Null coalescing mutation (remove right) | `builder.Configuration.GetConnectionString("GenApp")` | `builder.Configuration.GetConnectionString("GenApp")` |
| src/GenApp.Api/Program.cs:10 | NoCoverage | String mutation | `?? throw new InvalidOperationException("Connection string 'GenApp' is not configured."));` | `""` |
| src/GenApp.Api/Program.cs:17 | Survived | String mutation | `options.ClientErrorMapping[StatusCodes.Status400BadRequest].Link = "validation-error";` | `""` |
| src/GenApp.Api/Program.cs:20 | Survived | Statement mutation | `builder.Services.AddExceptionHandler<DatabaseExceptionHandler>();` | `;` |
| src/GenApp.Api/Program.cs:25 | Survived | Statement mutation | `app.UseExceptionHandler();` | `;` |
| src/GenApp.Api/Program.cs:28 | Survived | Negate expression | `if (app.Environment.IsDevelopment())` | `!(app.Environment.IsDevelopment())` |
| src/GenApp.Api/Program.cs:30 | Survived | Statement mutation | `app.UseSwagger();` | `;` |
| src/GenApp.Api/Program.cs:31 | Survived | Statement mutation | `app.UseSwaggerUI();` | `;` |
| src/GenApp.Api/Program.cs:36 | Survived | Statement mutation | `app.UseDefaultFiles();` | `;` |
| src/GenApp.Api/Program.cs:37 | Survived | Statement mutation | `app.UseStaticFiles();` | `;` |
| src/GenApp.Api/ResponseText.cs:7 | NoCoverage | String mutation | `public static string Of(string? value) => value?.TrimEnd() ?? "";` | `"Stryker was here!"` |
| src/GenApp.Api/ReturnCodeProblems.cs:14 | Survived | String mutation | `[ReturnCodes.NotFound] = new(StatusCodes.Status404NotFound, "not-found", "Kayıt bulunamadı` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:15 | Survived | String mutation | `[ReturnCodes.ConcurrencyConflict] = new(StatusCodes.Status409Conflict, "concurrency-confli` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:16 | Survived | String mutation | `[ReturnCodes.CustomerNotFound] = new(StatusCodes.Status404NotFound, "customer-not-found", ` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:17 | Survived | String mutation | `["80"] = new(StatusCodes.Status500InternalServerError, "storage-error", "Depolama hatası")` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:17 | Survived | String mutation | `["80"] = new(StatusCodes.Status500InternalServerError, "storage-error", "Depolama hatası")` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:17 | Survived | String mutation | `["80"] = new(StatusCodes.Status500InternalServerError, "storage-error", "Depolama hatası")` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:18 | Survived | String mutation | `["81"] = new(StatusCodes.Status500InternalServerError, "storage-error", "Depolama hatası")` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:18 | Survived | String mutation | `["81"] = new(StatusCodes.Status500InternalServerError, "storage-error", "Depolama hatası")` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:18 | Survived | String mutation | `["81"] = new(StatusCodes.Status500InternalServerError, "storage-error", "Depolama hatası")` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:19 | Survived | String mutation | `["82"] = new(StatusCodes.Status500InternalServerError, "storage-error", "Depolama hatası")` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:19 | Survived | String mutation | `["82"] = new(StatusCodes.Status500InternalServerError, "storage-error", "Depolama hatası")` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:19 | Survived | String mutation | `["82"] = new(StatusCodes.Status500InternalServerError, "storage-error", "Depolama hatası")` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:20 | Survived | String mutation | `["88"] = new(StatusCodes.Status500InternalServerError, "database-error", "Veritabanı hatas` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:20 | Survived | String mutation | `["88"] = new(StatusCodes.Status500InternalServerError, "database-error", "Veritabanı hatas` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:20 | Survived | String mutation | `["88"] = new(StatusCodes.Status500InternalServerError, "database-error", "Veritabanı hatas` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:21 | Survived | String mutation | `["89"] = new(StatusCodes.Status500InternalServerError, "database-error", "Veritabanı hatas` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:21 | Survived | String mutation | `["89"] = new(StatusCodes.Status500InternalServerError, "database-error", "Veritabanı hatas` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:21 | Survived | String mutation | `["89"] = new(StatusCodes.Status500InternalServerError, "database-error", "Veritabanı hatas` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:22 | Survived | String mutation | `[ReturnCodes.DatabaseError] = new(StatusCodes.Status500InternalServerError, "database-erro` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:29 | NoCoverage | Statement mutation | `throw new ArgumentOutOfRangeException(nameof(returnCode), returnCode, "No HTTP mapping for` | `;` |
| src/GenApp.Api/ReturnCodeProblems.cs:29 | NoCoverage | String mutation | `throw new ArgumentOutOfRangeException(nameof(returnCode), returnCode, "No HTTP mapping for` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:37 | NoCoverage | String mutation | `Create(StatusCodes.Status503ServiceUnavailable, "database-busy", "Veritabanı meşgul", Retu` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:37 | NoCoverage | String mutation | `Create(StatusCodes.Status503ServiceUnavailable, "database-busy", "Veritabanı meşgul", Retu` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:38 | NoCoverage | String mutation | `"Veritabanı şu an meşgul, isteği daha sonra yeniden deneyin.");` | `""` |
| src/GenApp.Api/ReturnCodeProblems.cs:41 | Survived | Object initializer mutation | `new ObjectResult(problem)` | `new ObjectResult(problem) {}` |
| src/GenApp.Api/ReturnCodeProblems.cs:44 | Survived | String mutation | `ContentTypes = { "application/problem+json" },` | `""` |
