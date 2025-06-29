using LTS.Base;
using LTS.Data.Repository;
using LTS.Models;
using LTS.Services;
using Microsoft.AspNetCore.Mvc;


namespace LTS.Pages.SearchPrepaidDetail
{
    public class IndexModel(PrepaidCardRepository cardRepo) : BasePageModel
    {
        [BindProperty]
        public string Code { get; set; } = string.Empty;
        public List<PrepaidCard> PrepaidCardList { get; set; } = new();

        public List<PrepaidCardUsage> PrepaidCardUsagesList { get; set; } = new();
        public async Task<IActionResult> OnGet()
        {
            try
            {
                var PhoneNumber = TempData["PhoneNumber"] as string;
                if (string.IsNullOrEmpty(PhoneNumber))
                {
                    return NoticeService.RedirectWithNotice(HttpContext, "잘못된 접근입니다", "/Home");
                }

                PrepaidCardList = await cardRepo.GetAllPrepaidCardsByPhoneNumberAsync(PhoneNumber);
                if (PrepaidCardList.Count == 0)
                {
                    return NoticeService.RedirectWithNotice(HttpContext, "해당 휴대전화 번호로 조회된 선불권이 존재하지 않습니다.", "/RefundPrepaid");
                }

                //갯수에 따라 분기 조절
                if (PrepaidCardList.Count == 1)
                {
                    var FirstCode = PrepaidCardList[0].Code;
                    TempData["Message"] = "선불권이 하나만 존재하여 자동으로 조회 되었습니다.";
                    PrepaidCardUsagesList = await cardRepo.GetPrepaidUsageListByCardCodeAsync(FirstCode);
                }
                HttpContext.Session.SetString("PhoneNumber", PhoneNumber);
                return Page();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[오류] 선불권 번호로 상세 조회 OnGet 중 예외 발생: {e.Message}\n{e.StackTrace}");
                TempData["Error"] = "선불권 상세조회 중 오류 발생";
                return Page();
            }
        }

        public async Task<IActionResult> OnPost()
        {
            try
            {
                if (string.IsNullOrEmpty(Code))
                {
                    return NoticeService.RedirectWithNotice(HttpContext, "코드 없이는 상세조회가 불가능합니다", "/Home");
                }
                //선불권 리스트 다시 로드
                var PhoneNumber = HttpContext.Session.GetString("PhoneNumber");
                if (string.IsNullOrWhiteSpace(PhoneNumber))
                {
                    TempData["Error"] = "선불권 상세조회 중 오류 발생 : 세션에 저장된 휴대폰 번호가 없습니다";
                    return Page();
                }
                PrepaidCardList = await cardRepo.GetAllPrepaidCardsByPhoneNumberAsync(PhoneNumber);

                // 코드로 선불권 사용 기록 조회
                PrepaidCardUsagesList = await cardRepo.GetPrepaidUsageListByCardCodeAsync(Code);
                TempData["Message"] = $"{Code} 조회 성공";
                return Page();
            }
            catch (Exception e)
            {
                Console.WriteLine($"[오류] 선불권 번호로 상세 조회 중 예외 발생: {e.Message}\n{e.StackTrace}");
                TempData["Error"] = "조회중 에러 발생";
                return Page();
            }
        }

    }
}