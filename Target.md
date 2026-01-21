# Technical Specification: Elasticsearch DSL to Lucene Conversion Logic


## 1. Introduction
Tài liệu này quy định các quy tắc chuyển đổi từ Elasticsearch Query DSL (JSON) sang Lucene Query Syntax (String) dành cho dự án migration hệ thống tìm kiếm sử dụng C#.

---

## 2. Mapping Rules: Term Level Queries

| Kiểu truy vấn | Elasticsearch DSL (JSON) | Lucene Syntax (String) | Ghi chú |
| :--- | :--- | :--- | :--- |
| **Term** | `{"term": {"user.id": "kimchy"}}` | `user.id:kimchy` | Tìm kiếm chính xác (không phân tích). |
| **Terms** | `{"terms": {"tag": ["search", "open"]}}` | `tag:(search OR open)` | Khớp một trong các giá trị trong mảng. |
| **Match** | `{"match": {"msg": "hello world"}}` | `msg:(hello world)` | Truy vấn toàn văn (Full-text). |
| **Match Phrase** | `{"match_phrase": {"msg": "hello world"}}` | `msg:"hello world"` | Tìm chính xác cụm từ theo thứ tự. |
| **Prefix** | `{"prefix": {"user": "ki"}}` | `user:ki*` | Tìm các giá trị bắt đầu bằng... |
| **Wildcard** | `{"wildcard": {"user": "k?m*"}}` | `user:k?m*` | `?` thay cho 1 ký tự, `*` cho nhiều ký tự. |
| **Fuzzy** | `{"fuzzy": {"user": "ki"}}` | `user:ki~2` | Tìm kiếm mờ (khoảng cách Levenshtein). |
| **Regexp** | `{"regexp": {"name": "s.*y"}}` | `name:/s.*y/` | Tìm kiếm theo biểu thức chính quy. |
| **Exists** | `{"exists": {"field": "user"}}` | `_exists_:user` | Kiểm tra trường không rỗng (null). |
| **IDs** | `{"ids": {"values": ["1", "4", "100"]}}` | `_id:("1" "4" "100")` | Tìm theo danh sách ID tài liệu. |

---

## 3. Mapping Rules: Range Queries

| Điều kiện | Elasticsearch DSL | Lucene Syntax | Mô tả |
| :--- | :--- | :--- | :--- |
| **Inclusive** | `{"range": {"age": {"gte": 10, "lte": 20}}}` | `age:[10 TO 20]` | Bao gồm cả 10 và 20. |
| **Exclusive** | `{"range": {"age": {"gt": 10, "lt": 20}}}` | `age:{10 TO 20}` | Không bao gồm 10 và 20. |
| **Half-open** | `{"range": {"age": {"gte": 10}}}` | `age:[10 TO *]` | Từ 10 trở lên. |
| **Date Math** | `{"range": {"date": {"gte": "now-1d/d"}}}` | `date:[now-1d/d TO *]` | Hỗ trợ các biểu thức thời gian. |

---

## 4. Mapping Rules: Compound/Bool Queries



| Logic | Elasticsearch Clause | Lucene Operator | Ví dụ Lucene |
| :--- | :--- | :--- | :--- |
| **AND** | `bool.must` | `+` hoặc `AND` | `+field1:A +field2:B` |
| **OR** | `bool.should` | `OR` hoặc `( )` | `(field1:A OR field1:B)` |
| **NOT** | `bool.must_not` | `-` hoặc `NOT` | `-field1:A` |
| **Filter** | `bool.filter` | `+` | Giống must nhưng không tính điểm (score). |

---

## 5. C# Implementation Notes
- **Recursive Pattern:** Cần sử dụng hàm đệ quy để duyệt cây JSON.
- **Escaping:** Các ký tự đặc biệt của Lucene (`+ - && || ! ( ) { } [ ] ^ " ~ * ? : \ /`) phải được xử lý thoát chuỗi bằng `\`.
- **Casing:** Các toán tử `AND`, `OR`, `NOT` bắt buộc phải viết hoa.

---

## 6. Acceptance Criteria (AC)
- **AC1:** Output string phải hợp lệ về mặt cú pháp Lucene.
- **AC2:** Xử lý chính xác các truy vấn lồng nhau (Nested boolean).
- **AC3:** Chuyển đổi đúng định dạng ngoặc `[]` và `{}` cho Range Query.
---

## 7. Ví dụ minh họa (Complex Example)

**Đầu vào (DSL):**
```json
{
  "query": {
    "bool": {
      "must": [{ "term": { "brand": "apple" } }],
      "should": [
        { "match": { "color": "red" } },
        { "match": { "color": "blue" } }
      ],
      "filter": [{ "range": { "price": { "lte": 500 } } }]
    }
  }
}
```

**Lucene Syntax Output:** +category:smartphone +(brand:apple OR brand:samsung) -condition:used


## 8. ĐẶC TẢ KỸ THUẬT: QUY TẮC CHUYỂN ĐỔI PHÂN CẤP VÀ LOGIC PHỨC HỢP

### 1. Quy tắc xử lý phân cấp (Nesting Rules)
Để đảm bảo tính đúng đắn khi chuyển đổi bằng C#, hệ thống cần tuân thủ các quy tắc xử lý chuỗi sau:



* **Đóng gói dấu ngoặc:** Mỗi khi gặp một khối `bool`, nội dung bên trong phải được bao bọc bởi cặp ngoặc đơn `()` để tránh sai lệch logic khi kết hợp với các điều kiện bên ngoài.
* **Toán tử mặc định:** * Nếu trong `bool` chỉ có `should`, các điều kiện con được nối với nhau bằng toán tử `OR`.
    * Nếu trong `bool` có sự xuất hiện đồng thời của `must`/`filter` và `should`, thì khối `should` phải nằm trong một cặp ngoặc riêng biệt và nối với `must` bằng toán tử `+`.
* **Xử lý mảng rỗng:** Nếu các mảng `must: []`, `should: []` hoặc `must_not: []` bị rỗng, trình chuyển đổi phải bỏ qua khối đó để tránh tạo ra các toán tử thừa (ví dụ: `+()`).

---

### 2. Ví dụ chuyển đổi phức hợp (Advanced Example)

> **Scenario:** Tìm sản phẩm thuộc ngành hàng "Điện thoại" **AND** (của hãng "Apple" **OR** "Samsung") **NOT** phải hàng "Cũ".

### Elasticsearch DSL (Đầu vào)
```json
{
  "query": {
    "bool": {
      "must": [
        { "term": { "category": "smartphone" } }
      ],
      "should": [
        { "term": { "brand": "apple" } },
        { "term": { "brand": "samsung" } }
      ],
      "must_not": [
        { "term": { "condition": "used" } }
      ],
      "minimum_should_match": 1
    }
  }
}