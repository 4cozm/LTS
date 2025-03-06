
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LTS.Pages
{
    
    public class IndexModel : PageModel
    {

        [BindProperty]
        [Required(ErrorMessage ="아이디를 입력해 주세요")]
        public string? Username { get; set; }

        [BindProperty]
        [Required(ErrorMessage ="비밀번호를 입력해 주세요")]
        public string? Password {get;set;}

        public string? FailedMessage { get; set; }
        public IActionResult OnPost(){
            Console.WriteLine("🔥 OnPost 실행됨!");
            if (!ModelState.IsValid)
            {
                return Page();
            }
            if(Username == "admin" && Password == "password")
            {
                return RedirectToPage("/Main");
            }
            ModelState.AddModelError("FailedMessage", "로그인 정보가 올바르지 않습니다.");
            return Page();
        }

    }
}