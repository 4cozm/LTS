using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Filters;
using LTS.Services;

namespace LTS.Base
{
    public abstract class BasePageModel : PageModel
    {
        public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            // 쿠키 기반 Notice 메시지를 TempData로 이관
            NoticeService.TransferToTempData(context.HttpContext, TempData);
        }
    }
}
