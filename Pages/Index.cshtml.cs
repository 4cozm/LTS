
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using LTS.Services;

namespace LTS.Pages
{
    public class IndexModel : PageModel
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
                var token = _loginService.TryLogin(Username, Password);
                return RedirectToPage("/Home");
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