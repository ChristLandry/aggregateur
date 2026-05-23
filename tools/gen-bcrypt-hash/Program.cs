var password = args.Length > 0 ? args[0] : "ChangeMe123!";
var hash = BCrypt.Net.BCrypt.HashPassword(password, 12);
Console.WriteLine(hash);
