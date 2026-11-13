$baseUrl = "http://localhost:5231/api"
$ErrorActionPreference = "Stop"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " BAT DAU KIEM THU TOAN DIEN HE THONG (E2E)" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# 1. Login Customer
Write-Host "`n[1] Dang nhap tai khoan Khach hang..." -ForegroundColor Yellow
$loginBody = @{
    email = "customer@gmail.com"
    password = "Customer@123"
} | ConvertTo-Json

$loginRes = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$token = $loginRes.data.accessToken
$customerId = $loginRes.data.user.customerId
Write-Host "✓ Dang nhap thanh cong! CustomerId: $customerId" -ForegroundColor Green

$authHeaders = @{
    Authorization = "Bearer $token"
}

# 2. Get Movies
Write-Host "`n[2] Lay danh sach phim dang chieu..." -ForegroundColor Yellow
$moviesRes = Invoke-RestMethod -Uri "$baseUrl/movies" -Method Get
$movie = $moviesRes.data[0]
Write-Host "✓ Chon phim: '$($movie.title)' (MovieId: $($movie.movieId))" -ForegroundColor Green

# 3. Get Showtimes for Movie
Write-Host "`n[3] Lay suat chieu cua phim..." -ForegroundColor Yellow
$today = (Get-Date).ToString("yyyy-MM-dd")
$showtimesRes = Invoke-RestMethod -Uri "$baseUrl/showtimes?movieId=$($movie.movieId)&date=$today" -Method Get
if ($showtimesRes.data.items.Count -eq 0) {
    # Thu lay tat ca suat chieu neu hom nay khong co
    $showtimesRes = Invoke-RestMethod -Uri "$baseUrl/showtimes?movieId=$($movie.movieId)" -Method Get
}
$showtime = $showtimesRes.data.items[0]
Write-Host "✓ Chon suat chieu: $($showtime.showtimeId) tai phong: $($showtime.roomName)" -ForegroundColor Green

# 4. Get Seats
Write-Host "`n[4] Lay so do ghe cua suat chieu..." -ForegroundColor Yellow
$seatsRes = Invoke-RestMethod -Uri "$baseUrl/showtimes/$($showtime.showtimeId)/seats" -Method Get
$availSeats = $seatsRes.data.seats | Where-Object { $_.status -eq "AVAILABLE" }
$selectedSeat = $availSeats[0]
Write-Host "✓ Chon ghe trong: $($selectedSeat.seatNumber) (SeatId: $($selectedSeat.seatId))" -ForegroundColor Green

# 5. Create Booking Attempt (Hold Seat)
Write-Host "`n[5] Tao luot giu ghe (Payment Attempt)..." -ForegroundColor Yellow
$holdBody = @{
    showtimeId = [int]$showtime.showtimeId
    seatIds = @([int]$selectedSeat.seatId)
} | ConvertTo-Json

$holdRes = Invoke-RestMethod -Uri "$baseUrl/payments/mock" -Method Post -Body $holdBody -Headers $authHeaders -ContentType "application/json"
$attemptId = $holdRes.data.paymentAttemptId
Write-Host "✓ Giu ghe thanh cong! Ma giu cho: #$attemptId, Thoi gian: $($holdRes.data.remainingSeconds)s" -ForegroundColor Green

# 6. Apply Promotion Code
Write-Host "`n[6] Ap dung ma giam gia CHAOBANMOI..." -ForegroundColor Yellow
$promoBody = @{
    code = "CHAOBANMOI"
} | ConvertTo-Json
$promoRes = Invoke-RestMethod -Uri "$baseUrl/customers/$customerId/bookings/$attemptId/promotion" -Method Post -Body $promoBody -Headers $authHeaders -ContentType "application/json"
Write-Host "✓ Ap ma giam gia thanh cong! Giam: $($promoRes.data.discountAmount)d, Tong con: $($promoRes.data.totalAmount)d" -ForegroundColor Green

# 7. Complete Payment (Mock)
Write-Host "`n[7] Thuc hien thanh toan mo phong (SUCCESS)..." -ForegroundColor Yellow
$payBody = @{
    result = "SUCCESS"
} | ConvertTo-Json
$payRes = Invoke-RestMethod -Uri "$baseUrl/payments/mock/$attemptId/complete" -Method Post -Body $payBody -Headers $authHeaders -ContentType "application/json"
$qrToken = $payRes.data.qrToken
Write-Host "✓ Thanh toan thanh cong! Sinh ma QR Token: $qrToken" -ForegroundColor Green

# 8. Check Customer Booking History
Write-Host "`n[8] Kiem tra lich su ve cua khach..." -ForegroundColor Yellow
$histRes = Invoke-RestMethod -Uri "$baseUrl/customers/$customerId/bookings/history" -Method Get -Headers $authHeaders
$latestBooking = $histRes.data[0]
Write-Host "✓ Don ve trong lich su: #$($latestBooking.bookingId), Trang thai: $($latestBooking.status)" -ForegroundColor Green

# 9. Admin Login & Check-in Ticket with QR
Write-Host "`n[9] Dang nhap tai khoan Admin & Soat ve..." -ForegroundColor Yellow
$adminLoginBody = @{
    email = "admin@cinema.com"
    password = "Admin@123"
} | ConvertTo-Json
$adminLoginRes = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method Post -Body $adminLoginBody -ContentType "application/json"
$adminToken = $adminLoginRes.data.accessToken
$adminHeaders = @{
    Authorization = "Bearer $adminToken"
}

# Scan QR
Write-Host "  -> Nhan vien quét QR Token tai quay..."
$scanRes = Invoke-RestMethod -Uri "$baseUrl/ticket-checking/qr/$qrToken" -Method Get -Headers $adminHeaders
Write-Host "✓ Quet QR thanh cong! Khach hang: $($scanRes.data.customerName), Ghe: $($scanRes.data.seats)" -ForegroundColor Green

# Print Ticket
Write-Host "  -> Nhan vien bam 'In ve'..."
$printRes = Invoke-RestMethod -Uri "$baseUrl/ticket-checking/qr/$qrToken/print" -Method Post -Headers $adminHeaders
Write-Host "✓ In ve thanh cong! Trang thai QR: $($printRes.data.qrStatus), Trang thai ve: $($printRes.data.printStatus)" -ForegroundColor Green

Write-Host "`n=======================================================" -ForegroundColor Cyan
Write-Host " TAT CA 9 BUOC KIEM THU END-TO-END DEU THANH CONG 100%! " -ForegroundColor Green
Write-Host "=======================================================" -ForegroundColor Cyan
