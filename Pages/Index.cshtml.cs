
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using LTS.Services;
using LTS.Base;
namespace LTS.Pages
{
    public class IndexModel : BasePageModel
    {
        private readonly LoginService _loginService;
        public IndexModel(LoginService loginService)
        {
            _loginService = loginService;
        }

        [BindProperty]
        [Required(ErrorMessage = "아이디를 입력해 주세요")]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "비밀번호를 입력해 주세요")]
        public string Password { get; set; } = string.Empty;

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            try
            {
                var session = _loginService.TryLogin(Username, Password);
                Response.Cookies.Append("LTS-Session", session.Token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    Expires = session.ExpireAt,
                    SameSite = SameSiteMode.Strict
                });
                HttpContext.Response.Redirect("/Home");
                return new EmptyResult();
            }
            catch (UnauthorizedAccessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }

            return Page();
        }

    }
}