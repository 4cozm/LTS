

--⚠️스키마 추가/변경시 Models에 있는 Dapper 타입 추론용 모델도 수정해야함.
USE lts_core;
-- 회원 정보 테이블
CREATE TABLE users (
    id INT AUTO_INCREMENT PRIMARY KEY,         -- 고유 사용자 ID
    password VARCHAR(255) NOT NULL,            -- 암호화된 비밀번호
    name VARCHAR(50),                          -- 사용자 이름
    phone VARCHAR(20),                         -- 전화번호
    address TEXT,                              -- 주소
    joined_at DATETIME DEFAULT CURRENT_TIMESTAMP, -- 가입일
    is_active BOOLEAN DEFAULT TRUE,            -- 계정 활성화 여부
    last_login DATETIME,                       -- 마지막 로그인 시각
    dirty_flag BOOLEAN DEFAULT FALSE           -- 수정 여부 플래그 (캐시 동기화 등 용도)
);

-- 쿠폰 원형 테이블 (쿠폰 정의)
CREATE TABLE coupons (
    id INT AUTO_INCREMENT PRIMARY KEY,         -- 쿠폰 ID
    code VARCHAR(50) NOT NULL UNIQUE,          -- 고유 쿠폰 코드
    description TEXT,                          -- 쿠폰 설명
    discount_type ENUM('PERCENT', 'FIXED') NOT NULL, -- 할인 방식: 비율 또는 고정금액
    discount_value DECIMAL(10,2) NOT NULL,     -- 할인 값 (퍼센트 또는 금액)
    min_order_amount DECIMAL(10,2),            -- 최소 주문 금액 조건
    valid_days INT NOT NULL DEFAULT 30,        -- 발급일 기준 유효기간 (일 단위, 기본 30일)
    is_active BOOLEAN DEFAULT TRUE             -- 쿠폰 사용 가능 여부
);

-- 사용자에게 발급된 쿠폰 테이블
CREATE TABLE user_coupons (
    id INT AUTO_INCREMENT PRIMARY KEY,         -- 발급된 쿠폰의 고유 ID
    user_id INT NOT NULL,                      -- 사용자 ID (users 테이블 참조)
    coupon_id INT NOT NULL,                    -- 쿠폰 ID (coupons 테이블 참조)
    assigned_at DATETIME DEFAULT CURRENT_TIMESTAMP, -- 쿠폰 발급 시각
    expires_at DATETIME NOT NULL,              -- 만료일 (발급일 + 유효일 수)
    used_at DATETIME,                          -- 실제 사용 시각 (NULL이면 미사용)
    is_used BOOLEAN DEFAULT FALSE,             -- 사용 여부
    dirty_flag BOOLEAN DEFAULT FALSE,          -- 수정 여부 플래그 (캐시 동기화 등 용도)

    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,   -- 유저 삭제 시 함께 삭제
    FOREIGN KEY (coupon_id) REFERENCES coupons(id) ON DELETE CASCADE, -- 쿠폰 삭제 시 함께 삭제

    UNIQUE(user_id, coupon_id)                 -- 같은 쿠폰을 한 번만 받을 수 있음
);

-- 쿠폰 사용 이력 테이블
CREATE TABLE coupon_usages (
    id INT AUTO_INCREMENT PRIMARY KEY,                 -- 로그 고유 ID
    user_coupon_id INT NOT NULL,                       -- 발급된 쿠폰 ID (user_coupons 참조)
    action_type ENUM('USE', 'CANCEL', 'RESTORE', 'TEST') NOT NULL DEFAULT 'USE',
    -- 수행된 동작 종류:
    -- 'USE'     : 실제 사용
    -- 'CANCEL'  : 사용 후 취소
    -- 'RESTORE' : 복구
    -- 'TEST'    : 테스트 또는 임시 사용 기록

    used_at DATETIME DEFAULT CURRENT_TIMESTAMP,        -- 사용 시각
    note TEXT,                                          -- 사용 사유나 설명 (예: 주문번호, 관리자 메모 등)

    FOREIGN KEY (user_coupon_id) REFERENCES user_coupons(id) ON DELETE CASCADE
);

-- 선불권 발급
CREATE TABLE prepaid_cards (
    id INT AUTO_INCREMENT PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,         -- 사용자 입력을 위한 고유 코드
    type ENUM('AMOUNT', 'COUNT') NOT NULL,    -- 금액형 / 횟수형
    initial_value DECIMAL(10,2) NOT NULL,     -- 최초 금액 또는 횟수
    remaining_value DECIMAL(10,2) NOT NULL,   -- 남은 금액 또는 횟수
    issued_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    expires_at DATETIME,                      -- 유효기간 (선택)
    is_active BOOLEAN DEFAULT TRUE,           -- 비활성화/차단 여부
    purchaser_name VARCHAR(100),              -- 비회원일 수 있으므로 이름만
    purchaser_contact VARCHAR(50),            -- 이메일 또는 전화번호 등
    notes TEXT,                               -- 기타 메모
    store_code VARCHAR(20)                    -- 발급 매장에 대한 이니셜
);

-- 선불권 사용 및 변경 내역 기록 테이블
CREATE TABLE prepaid_card_usages (
    id INT AUTO_INCREMENT PRIMARY KEY,             -- 로그 고유 ID
    prepaid_card_id INT NOT NULL,                  -- 대상 선불권 ID
    action_type ENUM('USE', 'RESTORE', 'ADJUST', 'TEST') NOT NULL DEFAULT 'USE',
    -- 수행된 행위의 종류:
    -- 'USE'     : 사용
    -- 'RESTORE' : 복원 (환불 등)
    -- 'ADJUST'  : 관리자 수동 조정
    -- 'TEST'    : 테스트

    change_amount DECIMAL(10,2) NOT NULL,          -- 사용 또는 복원된 양
    -- 양수: 잔액 차감 (사용)
    -- 음수: 잔액 증가 (복원, 취소)

    usage_note TEXT,                               -- 사용 내역 또는 메모
    used_at DATETIME DEFAULT CURRENT_TIMESTAMP,    -- 로그 기록 시각

    FOREIGN KEY (prepaid_card_id) REFERENCES prepaid_cards(id) ON DELETE CASCADE
);
