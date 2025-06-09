-- When adding or modifying schema, also update the Dapper model in the Models directory.
USE lts_core;

-- User information table
CREATE TABLE users (
    id INT AUTO_INCREMENT PRIMARY KEY,             -- Unique user ID
    password VARCHAR(255) NOT NULL,                -- Encrypted password
    name VARCHAR(50),                              -- User name
    phone VARCHAR(20),                             -- Phone number
    address TEXT,                                  -- Address
    joined_at DATETIME DEFAULT CURRENT_TIMESTAMP,  -- Join date
    is_active BOOLEAN DEFAULT TRUE,                -- Account active status
    last_login DATETIME,                           -- Last login timestamp
    dirty_flag BOOLEAN DEFAULT FALSE               -- Dirty flag for sync/cache purposes
);

-- Coupon definition table
CREATE TABLE coupons (
    id INT AUTO_INCREMENT PRIMARY KEY,             -- Coupon ID
    code VARCHAR(50) NOT NULL UNIQUE,              -- Unique coupon code
    description TEXT,                              -- Description
    discount_type ENUM('PERCENT', 'FIXED') NOT NULL, -- Discount type
    discount_value DECIMAL(10,2) NOT NULL,         -- Discount amount or percentage
    min_order_amount DECIMAL(10,2),                -- Minimum order amount
    valid_days INT NOT NULL DEFAULT 30,            -- Validity in days from issue date
    is_active BOOLEAN DEFAULT TRUE                 -- Coupon active status
);

-- User coupon assignment table
CREATE TABLE user_coupons (
    id INT AUTO_INCREMENT PRIMARY KEY,             -- Assigned coupon ID
    user_id INT NOT NULL,                          -- Reference to users table
    coupon_id INT NOT NULL,                        -- Reference to coupons table
    assigned_at DATETIME DEFAULT CURRENT_TIMESTAMP, -- Assignment time
    expires_at DATETIME NOT NULL,                  -- Expiration date
    used_at DATETIME,                              -- Used time (NULL = unused)
    is_used BOOLEAN DEFAULT FALSE,                 -- Usage status
    dirty_flag BOOLEAN DEFAULT FALSE,              -- Dirty flag

    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    FOREIGN KEY (coupon_id) REFERENCES coupons(id) ON DELETE CASCADE,

    UNIQUE(user_id, coupon_id)                     -- Prevent duplicate assignments
);

-- Coupon usage history table
CREATE TABLE coupon_usages (
    id INT AUTO_INCREMENT PRIMARY KEY,             -- Log ID
    user_coupon_id INT NOT NULL,                   -- Reference to user_coupons
    action_type ENUM('USE', 'CANCEL', 'RESTORE', 'TEST') NOT NULL DEFAULT 'USE',
    -- 'USE': actual usage
    -- 'CANCEL': usage cancelled
    -- 'RESTORE': restored
    -- 'TEST': test or mock usage

    used_at DATETIME DEFAULT CURRENT_TIMESTAMP,    -- Usage time
    note TEXT,                                     -- Usage notes

    FOREIGN KEY (user_coupon_id) REFERENCES user_coupons(id) ON DELETE CASCADE
);

-- Prepaid card issuance
CREATE TABLE prepaid_cards (
    id INT AUTO_INCREMENT PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,              -- Unique code for user entry
    type ENUM('AMOUNT', 'COUNT') NOT NULL,         -- Type: amount-based or count-based
    initial_value DECIMAL(10,2) NOT NULL,          -- Initial value
    remaining_value DECIMAL(10,2) NOT NULL,        -- Remaining value
    issued_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    expires_at DATETIME,                           -- Optional expiration date
    is_active BOOLEAN DEFAULT TRUE,                -- Active/block status
    purchaser_name VARCHAR(100),                   -- Purchaser name
    purchaser_contact VARCHAR(50),                 -- Email or phone
    notes TEXT,                                    -- Notes or memo
    store_code VARCHAR(20)                         -- Store identifier
);

-- Prepaid card usage and history
CREATE TABLE prepaid_card_usages (
    id INT AUTO_INCREMENT PRIMARY KEY,             -- Log ID
    prepaid_card_id INT NOT NULL,                  -- Reference to prepaid_cards
    action_type ENUM('USE', 'RESTORE', 'ADJUST', 'TEST') NOT NULL DEFAULT 'USE',
    -- 'USE': usage
    -- 'RESTORE': refund or restore
    -- 'ADJUST': admin adjustment
    -- 'TEST': test entry

    change_amount DECIMAL(10,2) NOT NULL,          -- Change in amount (positive = use, negative = restore)
    usage_note TEXT,                               -- Usage memo
    used_at DATETIME DEFAULT CURRENT_TIMESTAMP,    -- Timestamp

    FOREIGN KEY (prepaid_card_id) REFERENCES prepaid_cards(id) ON DELETE CASCADE
);

-- Employee table
CREATE TABLE employees (
    id INT AUTO_INCREMENT PRIMARY KEY,             -- Unique ID
    initials VARCHAR(10) NOT NULL UNIQUE,          -- Initials (e.g., AHG)
    name VARCHAR(255) NOT NULL,                    -- Name
    phone_number VARCHAR(20) NOT NULL,             -- Phone number
    password VARCHAR(255) NOT NULL,                -- Password
    is_password_changed BOOLEAN DEFAULT FALSE,     -- Whether initial password was changed
    store VARCHAR(50) NOT NULL,                    -- Store name or ID
    role_name VARCHAR(50) NOT NULL,                -- Role (staff, manager, owner)
    work_start_date DATE NOT NULL,                 -- Work start date
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,-- Creation timestamp
    created_by_member VARCHAR(20)                  -- Creator identifier
);
