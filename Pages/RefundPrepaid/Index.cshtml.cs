using LTS.Base;
using Microsoft.AspNetCore.Mvc;


namespace LTS.Pages.RefundPrepaid
{
    public class IndexModel : BasePageModel
    {
        [BindProperty]
        public string? PhoneNumber { get; set; }
    }
}