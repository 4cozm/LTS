USE lts_core;
SET NAMES utf8mb4;
-- User account table
CREATE TABLE users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    password VARCHAR(255) NOT NULL,                -- Hashed password
    name VARCHAR(50),                              -- Full name
    phone VARCHAR(20),                             -- Mobile number
    address TEXT,                                  -- User address
    joined_at DATETIME DEFAULT CURRENT_TIMESTAMP,  -- Registration timestamp
    is_active BOOLEAN DEFAULT TRUE,                -- Account status
    last_login DATETIME,                           -- Last login time
    dirty_flag BOOLEAN DEFAULT FALSE               -- For sync/cache tracking
);

-- Coupon master table
CREATE TABLE coupons (
    id INT AUTO_INCREMENT PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,              -- Unique coupon code
    description TEXT,                              -- Description of the coupon
    discount_type ENUM('PERCENT', 'FIXED') NOT NULL, -- Discount type
    discount_value DECIMAL(5,2) NOT NULL,          -- Discount amount or percentage
    min_order_amount DECIMAL(10,2),                -- Minimum order value required
    valid_days INT NOT NULL DEFAULT 30,            -- Validity period in days
    is_active BOOLEAN DEFAULT TRUE                 -- Active or inactive
);

-- Coupon assignment to users
CREATE TABLE user_coupons (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    coupon_id INT NOT NULL,
    assigned_at DATETIME DEFAULT CURRENT_TIMESTAMP, -- Time of assignment
    expires_at DATETIME NOT NULL,                  -- Expiration timestamp
    used_at DATETIME,                              -- Time of usage
    is_used BOOLEAN DEFAULT FALSE,                 -- Used or not
    dirty_flag BOOLEAN DEFAULT FALSE,

    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (coupon_id) REFERENCES coupons(id) ON DELETE CASCADE,
    UNIQUE(user_id, coupon_id)
);

-- Coupon usage logs
CREATE TABLE coupon_usages (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_coupon_id INT NOT NULL,
    action_type ENUM('USE', 'CANCEL', 'RESTORE', 'TEST') NOT NULL DEFAULT 'USE',
    used_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    note TEXT,

    FOREIGN KEY (user_coupon_id) REFERENCES user_coupons(id) ON DELETE CASCADE
);

-- Prepaid card definition
CREATE TABLE prepaid_cards (
    id INT AUTO_INCREMENT PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,              -- Unique code for entry
    type ENUM('AMOUNT', 'COUNT') NOT NULL,         -- Type of the card
    initial_value DECIMAL(10,2) NOT NULL,          -- Initial amount or count
    remaining_value DECIMAL(10,2) NOT NULL,        -- Remaining balance or count
    issued_at DATETIME DEFAULT CURRENT_TIMESTAMP,  -- Issue timestamp
    expires_at DATETIME NOT NULL,                  -- expiration date
    is_active BOOLEAN DEFAULT TRUE,                -- Active or inactive
    purchaser_name VARCHAR(100) NOT NULL,          -- Buyer name
    purchaser_contact VARCHAR(50) NOT NULL,        -- Buyer phone
    notes TEXT,                                    -- Optional notes
    store_code VARCHAR(20) NOT NULL                -- Associated store
);


CREATE TABLE prepaid_card_usage_state (
    id INT AUTO_INCREMENT PRIMARY KEY,
    prepaid_card_id INT NOT NULL UNIQUE,
    total_quantity DECIMAL(10,2) NOT NULL,
    used_quantity DECIMAL(10,2) NOT NULL DEFAULT 0,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP 
                 ON UPDATE CURRENT_TIMESTAMP,
    store_code VARCHAR(20) NOT NULL,

    FOREIGN KEY (prepaid_card_id) REFERENCES prepaid_cards(id) ON DELETE CASCADE
);

-- Prepaid card usage history
CREATE TABLE prepaid_card_usage_logs (
    id INT AUTO_INCREMENT PRIMARY KEY,
    prepaid_card_id INT NOT NULL,
    action_type ENUM('PURCHASE', 'USE', 'CANCEL', 'RESTORE', 'ADJUST', 'TEST') 
        NOT NULL DEFAULT 'USE',
    change_amount DECIMAL(10,2) NOT NULL,
    handler_name VARCHAR(50),
    reason VARCHAR(255),
    logged_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    store_code VARCHAR(20) NOT NULL,
    FOREIGN KEY (prepaid_card_id) REFERENCES prepaid_cards(id) ON DELETE CASCADE
);

-- Employee account table
CREATE TABLE employees (
    id INT AUTO_INCREMENT PRIMARY KEY,
    initials VARCHAR(10) NOT NULL UNIQUE,          -- Short initials (e.g., JSM)
    name VARCHAR(255) NOT NULL,                    -- Full name
    phone_number VARCHAR(20) NOT NULL,
    password VARCHAR(255) NOT NULL,
    is_password_changed BOOLEAN DEFAULT FALSE,     -- Whether password was updated
    store VARCHAR(50) NOT NULL,                    -- Store code
    role_name VARCHAR(50) NOT NULL,                -- Role (e.g., manager)
    work_start_date DATE NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by_member VARCHAR(20)                  -- Creator identifier
);

-- setup initial account
INSERT INTO employees (
    initials,
    name,
    phone_number,
    password,
    is_password_changed,
    store,
    role_name,
    work_start_date,
    created_at,
    created_by_member
) VALUES (
    'SYJ',
    '사장님',
    '01043067088',
    '$2a$11$qGIiFUo63QjmHBZmSmbDKeZzNNG1rHDaOd.wbgHR.S9myciUWZ.EK',
    0,
    'GA',
    'Owner',
    '2025-07-14',
    '2025-07-14 11:59:04',
    'System'
);
