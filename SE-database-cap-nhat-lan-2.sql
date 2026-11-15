/* =========================================================
   CẬP NHẬT LẦN 2 - phục vụ Backend Admin
   Chạy SAU "SE-database.sql" và "SE-database-cập nhật lần 1.sql".
   Có thể chạy lại nhiều lần (có kiểm tra tồn tại).
   ========================================================= */
USE Movie_Ticket_Booking_System;
GO

-- 1. Account: thêm trạng thái (xóa mềm = Inactive)
IF COL_LENGTH('Account', 'Status') IS NULL
    ALTER TABLE Account ADD Status NVARCHAR(50) NULL;
GO
UPDATE Account SET Status = N'Active' WHERE Status IS NULL;
GO

-- Email không được trùng (cho phép nhiều dòng NULL)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Account_Email' AND object_id = OBJECT_ID('Account'))
    CREATE UNIQUE INDEX UX_Account_Email ON Account(Email) WHERE Email IS NOT NULL;
GO

-- 2. Bảng Staff: nhân viên thuộc rạp nào
IF OBJECT_ID(N'Staff', N'U') IS NULL
BEGIN
    CREATE TABLE Staff
    (
        StaffID   INT IDENTITY(1,1) NOT NULL,
        UserID    INT               NOT NULL,
        CinemaID  INT               NULL,

        CONSTRAINT PK_Staff PRIMARY KEY (StaffID),
        CONSTRAINT FK_Staff_Account FOREIGN KEY (UserID) REFERENCES Account(UserID),
        CONSTRAINT FK_Staff_Cinema  FOREIGN KEY (CinemaID) REFERENCES Cinema(CinemaID),
        CONSTRAINT UQ_Staff_User UNIQUE (UserID)
    );
END
GO

-- 3. Room: sức chứa + mô tả
IF COL_LENGTH('Room', 'Capacity') IS NULL
    ALTER TABLE Room ADD Capacity INT NULL;
GO
IF COL_LENGTH('Room', 'Description') IS NULL
    ALTER TABLE Room ADD [Description] NVARCHAR(255) NULL;
GO

-- 4. Seat: loại ghế + không cho trùng vị trí trong cùng phòng
IF COL_LENGTH('Seat', 'SeatType') IS NULL
    ALTER TABLE Seat ADD SeatType NVARCHAR(50) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Seat_Room_Position' AND object_id = OBJECT_ID('Seat'))
    CREATE UNIQUE INDEX UX_Seat_Room_Position ON Seat(RoomID, [Row], [Column]);
GO

-- 5. Showtime_Seat: mỗi ghế chỉ có 1 dòng trong 1 suất chiếu
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ShowtimeSeat' AND object_id = OBJECT_ID('Showtime_Seat'))
    CREATE UNIQUE INDEX UX_ShowtimeSeat ON Showtime_Seat(ShowtimeID, SeatID);
GO
