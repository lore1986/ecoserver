// using Microsoft.IdentityModel.Tokens;
// using Newtonsoft.Json.Linq;
// using System;
// using System.IdentityModel.Tokens.Jwt;
// using System.Security.Claims;
// using System.Security.Cryptography;
// using System.Text;

// namespace webapi.Utilities
// {
//     public static class TokenValidation
//     {
//         public static string GenerateJwtToken(string identify, byte[] keygen)
//         {
//             var mySecurityKey = new SymmetricSecurityKey(keygen);
//             mySecurityKey.KeyId = identify;
//             //var myIssuer = "http://mysite.com";
//             //var myAudience = "http://myaudience.com";

//             var tokenHandler = new JwtSecurityTokenHandler();
//             var tokenDescriptor = new SecurityTokenDescriptor
//             {
//                 Subject = new ClaimsIdentity(new Claim[]
//                 {
//                     new Claim(ClaimTypes.Name, identify),
//                 }),
//                 Issuer = "ecodrone",
//                 Expires = DateTime.UtcNow.AddDays(1),
//                 //Issuer = myIssuer,
//                 //Audience = myAudience,
//                 SigningCredentials = new SigningCredentials(mySecurityKey, SecurityAlgorithms.HmacSha256Signature)
//             };

//             var token = tokenHandler.CreateToken(tokenDescriptor);
//             return tokenHandler.WriteToken(token);
//             /*var tokenHandler = new JwtSecurityTokenHandler();
//             var tokenDescriptor = new SecurityTokenDescriptor
//             {
//                 Subject = new ClaimsIdentity(new Claim[]
//                 {
//                     new Claim(ClaimTypes.Name, identify)
//                 }),
//                 Expires = DateTime.UtcNow.AddDays(1),
//                 SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keygen), SecurityAlgorithms.HmacSha256Signature)
//             };
//             var token = tokenHandler.CreateToken(tokenDescriptor);
//             return tokenHandler.WriteToken(token);*/
//         }

//         public static byte[] Generate256BitKey()
//         {
//             using (Aes aesAlgorithm = Aes.Create())
//             {
//                 aesAlgorithm.KeySize = 256;
//                 aesAlgorithm.GenerateKey();
//                 string keyBase64 = Convert.ToBase64String(aesAlgorithm.Key);
                
//                 return Convert.FromBase64String(keyBase64);
//             }
//         }


//         /*using (var rng = RandomNumberGenerator.Create())
//                {

//                    byte[] key = new byte[32]; // 256 bits is 32 bytes
//                    rng.GetBytes(key);

//                    return key;
//                }*/

//         public static ClaimsPrincipal ValidateJwtToken(string jwtToken, byte[] key)
//         {
//             /*var tokenHandler = new JwtSecurityTokenHandler();
//             //var key = Generate256BitKey(); // Use the same key as when generating the token
//             var hmac = new HMACSHA256(key);

//             var tokenValidationParameters = new TokenValidationParameters
//             {
//                 ValidateIssuerSigningKey = true,
//                 IssuerSigningKey = new SymmetricSecurityKey(hmac.Key),
//                 ValidateIssuer = true,
//                 ValidateAudience = true,
//                 //ClockSkew = TimeSpan.Zero // You can adjust the allowed clock skew if needed
//             };

//             SecurityToken validatedToken;
//             var principal = tokenHandler.ValidateToken(jwtToken, tokenValidationParameters, out validatedToken);

//             return principal;*/

//             var mySecurityKey = new SymmetricSecurityKey(key);

//             //var myIssuer = "http://mysite.com";
//             //var myAudience = "http://myaudience.com";

//             var tokenHandler = new JwtSecurityTokenHandler();
//             return tokenHandler.ValidateToken(jwtToken, new TokenValidationParameters
//             {
//                 ValidateIssuerSigningKey = false,
//                 /*ValidateIssuer = true,
//                 ValidateAudience = true,*/
//                 //TryAllIssuerSigningKeys = true,
//                 ValidIssuer = "ecodrone",
//                 ValidateAudience = false,
//                 //IssuerSigningKey = mySecurityKey
//             }, out SecurityToken validatedToken);
//         }
//     }
// }
