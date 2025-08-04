using Dapper;
using LTS.Models;
using LTS.Models.Base;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;

namespace LTS.Data.Repository;

public class PrepaidCardRepository
{
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
                (prepaid_card_id, action_type, change_amount, usage_note, store_code, used_at)
            VALUES 
                (@PrepaidCardId, @ActionType, @ChangeAmount, @UsageNote, @StoreCode, @UsedAt);";

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
                    (prepaid_card_id, action_type, change_amount, usage_note, store_code, used_at)
                VALUES 
                    (@PrepaidCardId, @ActionType, @ChangeAmount, @UsageNote, @StoreCode, @UsedAt)";

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

    //DB에서 활성화 된 카드만 가져옴
    public List<PrepaidCard> GetPrepaidCardByPhoneNumber(string phoneNumber)
    {
        try
        {
            using var conn = DbManager.GetConnection();
            if (conn == null) return new List<PrepaidCard>();

            string query = @"
            SELECT id AS Id, code AS Code, type AS Type,
                   initial_value AS InitialValue, remaining_value AS RemainingValue,
                   issued_at AS IssuedAt, expires_at AS ExpiresAt,
                   is_active AS IsActive, purchaser_name AS PurchaserName,
                   purchaser_contact AS PurchaserContact, notes AS Notes,
                   store_code AS StoreCode
            FROM prepaid_cards
            WHERE purchaser_contact = @PhoneNumber
              AND is_active = TRUE
            ORDER BY issued_at DESC";

            return conn.Query<PrepaidCard>(query, new { PhoneNumber = phoneNumber }).ToList();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[조회 오류] {ex.Message}");
            throw new InvalidOperationException("선불권 목록 조회 중 오류가 발생했습니다.", ex);
        }
    }
    public PrepaidCard? GetPrepaidCardByCode(string code)
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
            WHERE code = @Code
            LIMIT 1";

            return conn.QueryFirstOrDefault<PrepaidCard>(query, new { Code = code });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[조회 오류] {ex.Message}");
            throw new InvalidOperationException("선불권 조회 중 오류가 발생했습니다.", ex);
        }
    }

    public PrepaidCard UsePrepaidCardByCode(string code, int useAmount, PrepaidCardUsageLog usage)
    {
        try
        {
            using var conn = DbManager.GetConnection();
            if (conn == null) throw new InvalidOperationException("DB 연결에 실패했습니다.");

            using var tx = conn.BeginTransaction();

            // 카드 조회
            string selectQuery = @"
            SELECT id AS Id, code AS Code, type AS Type,
                   initial_value AS InitialValue, remaining_value AS RemainingValue,
                   issued_at AS IssuedAt, expires_at AS ExpiresAt,
                   is_active AS IsActive, purchaser_name AS PurchaserName,
                   purchaser_contact AS PurchaserContact, notes AS Notes,
                   store_code AS StoreCode
            FROM prepaid_cards
            WHERE code = @Code
            LIMIT 1";

            var card = conn.QueryFirstOrDefault<PrepaidCard>(selectQuery, new { Code = code }, tx);
            if (card == null)
                throw new InvalidOperationException("선불권을 찾을 수 없습니다.");

            // 사용 처리
            card.RemainingValue -= useAmount;
            if (card.RemainingValue <= 0)
            {
                card.RemainingValue = 0;
                card.IsActive = false;
            }

            string updateQuery = @"
            UPDATE prepaid_cards
            SET remaining_value = @RemainingValue,
                is_active = @IsActive
            WHERE id = @Id";

            conn.Execute(updateQuery, new { card.RemainingValue, card.IsActive, card.Id }, tx);

            // 로그 기록
            usage.PrepaidCardId = card.Id;
            string insertLogQuery = @"
            INSERT INTO prepaid_card_usage_logs
                (prepaid_card_id, action_type, change_amount, handler_name, store_code, logged_at)
            VALUES
                (@PrepaidCardId, @ActionType, @ChangeAmount, @HandlerName, @StoreCode, @LoggedAt)";

            conn.Execute(insertLogQuery, usage, tx);

            string insertUsageStateQuery = @"
            INSERT INTO prepaid_card_usage_state
                (prepaid_card_id, used_quantity, remaining_quantity)
            VALUES
                (@PrepaidCardId, @UsedQuantity, @RemainingQuantity)";

            conn.Execute(insertUsageStateQuery, new
            {
                PrepaidCardId = card.Id,
                RemainingQuantity = usage.ChangeAmount,
                UsedQuantity = usage.ChangeAmount,
            }, tx);

            tx.Commit();
            return card;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[선불권 사용 오류] {ex.Message}");
            throw new InvalidOperationException("선불권 사용 처리 중 오류가 발생했습니다.", ex);
        }
    }
    public async Task<List<PrepaidCardUsageViewModel>> GetPrepaidUsageWithUserInfoAsync(string storeCode)
    {
        try
        {
            using var conn = DbManager.GetConnection();
            if (conn == null)
                throw new InvalidOperationException("DB 연결에 실패했습니다.");

            string query = @"
            SELECT 
                u.id AS UsageId,
                u.action_type AS ActionType,
                u.change_amount AS ChangeAmount,
                u.usage_note AS UsageNote,
                u.used_at AS UsedAt,
                c.code AS PrepaidCardCode,
                c.id AS PrepaidCardId,
                c.purchaser_name AS PurchaserName,
                c.purchaser_contact AS PurchaserContact
            FROM prepaid_card_usages u
            JOIN prepaid_cards c ON u.prepaid_card_id = c.id
            WHERE u.store_code = @StoreCode
              AND u.used_at >= NOW() - INTERVAL 1 DAY
              AND u.action_type IN ('USE', 'RESTORE')
            ORDER BY u.used_at DESC";

            var result = await conn.QueryAsync<PrepaidCardUsageViewModel>(query, new { StoreCode = storeCode });
            return result.ToList();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[오류] 선불권 사용 이력 조회 실패: {ex.Message}");
            return new List<PrepaidCardUsageViewModel>(); // 또는 throw;
        }
    }

    // 선불권 취소 관련 로직


    public async Task<decimal?> GetChangeAmountByUsageIdAsync(string usageId)
    {
        using var conn = DbManager.GetConnection();
        if (conn == null) return null;
        //취소 개수가 사용 개수보다 많을 경우의 문제를 해결하기 위한 쿼리
        const string query = @"
            SELECT change_amount
            FROM prepaid_card_usages
            WHERE id = @UsageId";

        return await conn.QueryFirstOrDefaultAsync<decimal?>(query, new { UsageId = usageId });
    }

    public async Task<PrepaidCardSummary> RestorePrepaidCardUsageAsync(string usageId, decimal cancelAmount, string cardCode, PrepaidCardUsage usageLog)
    {
        using var conn = DbManager.GetConnection();
        if (conn == null) throw new InvalidOperationException("DB 연결에 실패했습니다.");

        using var transaction = conn.BeginTransaction();

        try
        {
            // 1. 기존 usage 조회
            var original = await conn.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT change_amount, prepaid_card_id FROM prepaid_card_usages WHERE id = @UsageId FOR UPDATE",
                new { UsageId = usageId }, transaction);

            if (original == null)
                throw new InvalidOperationException("선불권 복구중 에러 발생 : UsageId로 DB 조회에 실패했습니다.(로직 오류)");

            usageLog.PrepaidCardId = original.prepaid_card_id;
            decimal remaining = original.change_amount - cancelAmount;
            string newAction = remaining == 0 ? "ADJUST" : "USE";

            // 2. 기존 usage 수정
            await conn.ExecuteAsync(
                @"UPDATE prepaid_card_usages
              SET change_amount = @Remaining,
                  action_type = @ActionType
              WHERE id = @UsageId",
                new
                {
                    Remaining = remaining,
                    ActionType = newAction,
                    UsageId = usageId
                }, transaction);

            // 3. 카드 업데이트
            await conn.ExecuteAsync(
                @"UPDATE prepaid_cards
              SET remaining_value = remaining_value + @RestoreAmount,
                  is_active = 1
              WHERE code = @CardCode",
                new
                {
                    RestoreAmount = cancelAmount,
                    CardCode = cardCode
                }, transaction);

            // 4. 복구 로그 삽입
            await conn.ExecuteAsync(
                @"INSERT INTO prepaid_card_usages
              (prepaid_card_id, action_type, change_amount, usage_note, store_code, used_at)
              VALUES
              (@PrepaidCardId, @ActionType, @ChangeAmount, @UsageNote, @StoreCode, @UsedAt)",
                usageLog, transaction);

            var card = await conn.QuerySingleAsync<PrepaidCardSummary>(
            @"SELECT 
                purchaser_name AS PurchaserName,
                purchaser_contact AS PurchaserContact,
                remaining_value AS RemainingValue
            FROM prepaid_cards
            WHERE code = @Code",
            new { Code = cardCode }, transaction);

            transaction.Commit();


            return card;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<List<PrepaidCard>> GetAllPrepaidCardsByPhoneNumberAsync(string phoneNumber)
    {
        try
        {
            using var conn = DbManager.GetConnection();
            if (conn == null) return new List<PrepaidCard>();

            string query = @"
            SELECT id AS Id, code AS Code, type AS Type,
                   initial_value AS InitialValue, remaining_value AS RemainingValue,
                   issued_at AS IssuedAt, expires_at AS ExpiresAt,
                   is_active AS IsActive, purchaser_name AS PurchaserName,
                   purchaser_contact AS PurchaserContact, notes AS Notes,
                   store_code AS StoreCode
            FROM prepaid_cards
            WHERE purchaser_contact = @PhoneNumber
            ORDER BY issued_at DESC";

            var result = await conn.QueryAsync<PrepaidCard>(query, new { PhoneNumber = phoneNumber });
            return result.ToList();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[조회 오류] {ex.Message}");
            throw new InvalidOperationException("전화번호로 선불권 전체 목록 조회 중 오류가 발생했습니다.", ex);
        }
    }

    public async Task<List<PrepaidCardUsage>> GetPrepaidUsageListByCardCodeAsync(string cardCode)
    {
        using var conn = DbManager.GetConnection();
        if (conn == null)
            throw new InvalidOperationException("DB 연결에 실패했습니다.");

        string query = @"
        SELECT u.id AS Id,
               u.prepaid_card_id AS PrepaidCardId,
               u.action_type AS ActionType,
               u.change_amount AS ChangeAmount,
               u.usage_note AS UsageNote,
               u.store_code AS StoreCode,
               u.used_at AS UsedAt
        FROM prepaid_card_usages u
        JOIN prepaid_cards c ON u.prepaid_card_id = c.id
        WHERE c.code = @CardCode
        ORDER BY u.used_at DESC";

        var result = await conn.QueryAsync<PrepaidCardUsage>(query, new { CardCode = cardCode });
        return result.ToList();
    }

}



