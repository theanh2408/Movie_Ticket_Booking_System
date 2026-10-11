// Lỗi chuẩn của API. `fields` = lỗi theo từng ô nhập ({ Title: 'Bắt buộc' }), `data` = dữ liệu kèm theo (vd: danh sách suất chiếu xung đột).
export class ApiError extends Error {
  constructor(message, { status = 400, code = 'ERROR', fields = null, data = null } = {}) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.code = code;
    this.fields = fields;
    this.data = data;
  }
}
