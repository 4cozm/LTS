using Dapper;
using LTS.Models;
using MySql.Data.MySqlClient;

namespace LTS.Data.Repository;

public class PrepaidCardRepository
{
    public PrepaidCard? GetByPhoneNumber(string phoneNumber)
    {
        try
        {
            using var conn = DbManager.GetConnection();
            if (conn == null) return null;

            string query = @"
                SELECT id AS Id, code AS Code, type AS Type,
                       initial_value AS InitialValue, remaining_value AS RemainingValue,
                       issued_at AS IssuedAt, expires_at AS ExpiresAt,
                       is_active AS IsActive, purchaser_name AS PurchaserName,
                       purchaser_contact AS PurchaserContact, notes AS Notes,
                       store_code AS StoreCode
                FROM prepaid_cards
                WHERE purchaser_contact = @PhoneNumber
                ORDER BY issued_at DESC
                LIMIT 1";

            return conn.QueryFirstOrDefault<PrepaidCard>(query, new { PhoneNumber = phoneNumber });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[조회 오류] {ex.Message}");
            throw new InvalidOperationException("선불권 조회 중 오류가 발생했습니다.", ex);
        }
    }

    public PrepaidCard CreatePrepaidCardWithUsage(PrepaidCard card, PrepaidCardUsage usage)
    {
        try
        {
            using var conn = DbManager.GetConnection();
            if (conn == null)
                throw new InvalidOperationException("DB 연결에 실패했습니다.");

            using var tx = conn.BeginTransaction();

            // 카드 먼저 저장 + Id 추출
            string cardQuery = @"
            INSERT INTO prepaid_cards 
                (code, type, initial_value, remaining_value, issued_at, expires_at, is_active,
                 purchaser_name, purchaser_contact, notes, store_code)
            VALUES 
                (@Code, @Type, @InitialValue, @RemainingValue, @IssuedAt, @ExpiresAt, @IsActive,
                 @PurchaserName, @PurchaserContact, @Notes, @StoreCode);
            SELECT LAST_INSERT_ID();";

            var newCardId = conn.QuerySingle<int>(cardQuery, card, tx);
            card.Id = newCardId;

            // usage 기록 추가
            string usageQuery = @"
            INSERT INTO prepaid_card_usages 
                (prepaid_card_id, action_type, change_amount, usage_note, used_at)
            VALUES 
                (@PrepaidCardId, @ActionType, @ChangeAmount, @UsageNote, @UsedAt);";

            usage.PrepaidCardId = newCardId;
            conn.Execute(usageQuery, usage, tx);

            tx.Commit();

            return card;
        }
        catch (MySqlException ex)
        {
            Console.Error.WriteLine($"[MySQL 오류] {ex.Message}");
            throw new InvalidOperationException("선불권 생성 중 DB 오류가 발생했습니다.", ex);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[알 수 없는 오류] {ex.Message}");
            throw new InvalidOperationException("선불권 생성 중 알 수 없는 오류가 발생했습니다.", ex);
        }
    }

    public void LogUsage(PrepaidCardUsage usage)
    {
        try
        {
            using var conn = DbManager.GetConnection();
            if (conn == null)
                throw new InvalidOperationException("DB 연결에 실패했습니다.");

            string query = @"
                INSERT INTO prepaid_card_usages 
                    (prepaid_card_id, action_type, change_amount, usage_note, used_at)
                VALUES 
                    (@PrepaidCardId, @ActionType, @ChangeAmount, @UsageNote, @UsedAt)";

            int result = conn.Execute(query, usage);

            if (result == 0)
                throw new InvalidOperationException("사용 로그 기록에 실패했습니다.");
        }
        catch (MySqlException ex)
        {
            Console.Error.WriteLine($"[MySQL 오류] {ex.Message}");
            throw new InvalidOperationException("사용 기록 중 DB 오류가 발생했습니다.", ex);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[알 수 없는 오류] {ex.Message}");
            throw new InvalidOperationException("사용 기록 중 알 수 없는 오류가 발생했습니다.", ex);
        }
    }
}
