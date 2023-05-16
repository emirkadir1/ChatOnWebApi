using ChatOnWebApi.Models;
using ChatOnWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace ChatOnWebApi.Controllers
{

        [ApiController]
        [Route("api/[controller]")]
        public class UserController : ControllerBase
        {
        private readonly UserDbContext _context;
            public UserController(UserDbContext context)
            {
                _context = context;
            }

            [Authorize]
            [HttpGet("users")]
            public async Task<ActionResult<IEnumerable<User>>> GetUsers()
            {

                return await _context.Users.ToListAsync();
            }
            [HttpGet("{userName}")]
            public async Task<ActionResult<User>> GetUser(string userName)
            {
            var refreshToken = Request.Cookies["refreshToken"];
            var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserName == userName);          
                if (user == null)
                {
                    return NotFound();
                }
            UserResponse response = new()
            {
                UserName = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                ImageUrl = user.ImageUrl,
                PhoneNumber = user.PhoneNumber,
                BirthDay = user.BirthDay.ToString("d")
        };
            return Ok(response);
            }

        //Register
            [HttpPost("register")]
            public async Task<IActionResult> Register(UserRegisterRequest request)
            {
                if (_context.Users.Any(u => u.Email == request.Email))
                {
                    return BadRequest("Bu email kullanılmakta!");
                }
                if(_context.Users.Any(x=>x.UserName== request.UserName))
            {
                return BadRequest("Bu kullanıcı adı kullanılmakta!");
            }
                CreatePasswordHash(request.Password, out byte[] passwordHash, out byte[] passwordSalt);
                User user = new User
                {
                    UserName = request.UserName,
                    Email = request.Email,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    
                };
                if (user is User && user is not null)
                {
                 _context.Users.Add((User)user);

                var refreshToken = GenerateRefreshToken();
                RefreshToken token=SetRefreshToken(refreshToken);
                user.TokenExpires = token.Expires;
                user.TokenCreated = token.Created;
                user.RefreshToken = token.Token;
                token.User = user;
                _context.RefreshTokens.Add(token);
                NotificationList notificationList = new()
                {
                    User = user,
                };
                _context.NotificationList.Add(notificationList);
                await _context.SaveChangesAsync();
                return Ok(CreateRandomToken(user));
                }
            return BadRequest();
                
            }
        //Login
            [HttpPost("login")]
            public async Task<IActionResult> Login(UserLoginRequest request)
            {

                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == request.UserName.Trim());
                if (user == null)
                {
                    return BadRequest("Kullanıcı Bulunamadı");
                }
                if (!VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
                {
                    return BadRequest("Yanlış şifre!");
                }
            var refreshToken = GenerateRefreshToken();
            RefreshToken token = SetRefreshToken(refreshToken);
            user.TokenExpires = token.Expires;
            user.TokenCreated = token.Created;
            user.RefreshToken = token.Token;
            token.User = user;
            _context.RefreshTokens.Add(token);
            await _context.SaveChangesAsync();
            return Ok(CreateRandomToken(user));
            }
        //Auto Login
        [HttpGet("auto-login")]
        public async Task<IActionResult> AutomaticLogin()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            var setRefreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .SingleOrDefaultAsync(u => u.Token == refreshToken);
            if (setRefreshToken == null || setRefreshToken.Expires<DateTime.Now)
            {
                return Unauthorized();
            }
            var user =  await _context.Users.SingleOrDefaultAsync(x => x.Id == setRefreshToken.User.Id);
            if(user != null)
            {
                setRefreshToken.Expires = DateTime.Now.AddDays(14);
                user.TokenExpires.AddDays(14);
                await _context.SaveChangesAsync();
                return Ok(CreateRandomToken(user));
            }
            return BadRequest();
        }
        [HttpGet("delete-refresh-token")]
        public async Task<IActionResult> DeleteRefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            var setRefreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .SingleOrDefaultAsync(u => u.Token == refreshToken);
            
            if (setRefreshToken != null)
            {
                var user = await _context.Users.SingleOrDefaultAsync(x => x.Id == setRefreshToken.User.Id);
                if(user != null ) 
                    user.Online = false;
                _context.RefreshTokens.Remove(setRefreshToken);
                Response.Cookies.Delete("refreshToken");
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost("firsttime")]
        public async Task<IActionResult> FirsTimeSetProfile(UserFirstTimeProfile request)
        {

            var user = await _context.Users.SingleOrDefaultAsync(x => x.UserName == request.UserName);
            if (user.UserName == request.UserName && user is not null)
            {
                user.FirstName = request.FirstName;
                user.LastName = request.LastName;
                user.BirthDay = request.BirthDate;
                user.PhoneNumber = request.PhoneNumber;
                user.ImageUrl= request.ImageUrl;
                user.LanguageCode = request.LanguageCode;
                _context.SaveChanges();
                return Ok("Başarıyla Güncelleme Gerçekleşti.");
            }
            return BadRequest();
        }

        [HttpPost("setprofile")]
        public async Task<IActionResult> SetProfile(UserSetProfileRequest request)
        {
            
            var user = await _context.Users.SingleOrDefaultAsync(x => x.UserName == request.UserName);
            if (user.UserName == request.UserName && user is not null)
            {
                user.FirstName = request.FirstName;
                user.LastName = request.LastName;
                user.BirthDay = request.BirthDate;
                user.PhoneNumber = request.PhoneNumber;
                user.Email = request.Email;
                await _context.SaveChangesAsync();
                return Ok("Başarıyla Güncelleme Gerçekleşti.");
            } 
            return BadRequest();
        }
        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
            {
                using (var hmac = new HMACSHA512())
                {
                    passwordSalt = hmac.Key;
                    passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                }
            }
            private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
            {
                using (var hmac = new HMACSHA512(passwordSalt))
                {
                    var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                    return computedHash.SequenceEqual(passwordHash);
                }
            }


        private string CreateRandomToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
            };
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("my top secret key"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            var token = new JwtSecurityToken(
                claims:claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds);
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            return jwt;
        }
        private RefreshToken GenerateRefreshToken()
        {
            var refreshToken = new RefreshToken()
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                Expires = DateTime.Now.AddDays(14),
                Created = DateTime.Now
            };
            return refreshToken;
        }
        private RefreshToken SetRefreshToken(RefreshToken newRefreshToken)
        {
            var cookieOptions = new CookieOptions()
            {
                HttpOnly = true,
                Expires = newRefreshToken.Expires,
                SameSite = SameSiteMode.None
            };
            Response.Cookies.Append("refreshToken", newRefreshToken.Token, cookieOptions);

            return newRefreshToken;
        }
        [HttpPost("refresh-token")]
        public async Task<ActionResult<string>> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            HttpContext.Request.Headers.TryGetValue("jwtToken",out var HeaderJwt);
            var userName = TokenService.GetName(HeaderJwt);
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserName == userName);
            if (!user.RefreshToken.Equals(refreshToken))
            {
                return Unauthorized("Invalid RefreshToken");
            }
            else if (user.TokenExpires < DateTime.Now)
            {
                return Unauthorized("Token expired");
            }
            var setRefreshToken =await _context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refreshToken);
            setRefreshToken=SetRefreshToken(GenerateRefreshToken());
            user.RefreshToken = setRefreshToken.Token;
            user.TokenExpires = setRefreshToken.Expires;
            user.TokenCreated = setRefreshToken.Created;
            await _context.SaveChangesAsync();
            return CreateRandomToken(user);

        }

    }
}

