# Danh mục API — theo code hiện tại (`Juratifact.API/Controller`)

Tài liệu được sinh từ attribute `[Route]` / `[Http*]` trong code. Base mặc định là **`api/[controller]`**: token `[controller]` = tên class **bỏ hậu tố `Controller`**, giữ **PascalCase** (ví dụ `ProductController` → `api/Product`).

Routing ASP.NET Core **không phân biệt hoa thường** trên URL; client có thể gọi `api/product` hoặc `api/Product`.

**Ngoại lệ:** `DisputeController.CreateDispute` dùng template **`~/api/orders/{orderId}/disputes`** (đường dẫn tuyệt đối, không gắn prefix `api/Dispute`).

---

## So sánh tên / đường dẫn: **trước** → **sau**

*Trước* = kiểu URL cũ (PascalCase trong path, `create` / `Post` / v.v.). *Sau* = code hiện tại trong `Juratifact.API/Controller`. Base vẫn là `api/[controller]` (ví dụ `api/Product`) trừ khi ghi rõ khác.

### Product

| Method | Trước | Sau |
|--------|--------|-----|
| GET | `api/Product` hoặc `api/Product/` | `api/Product` |
| GET | `api/Product/MyProducts` | `api/Product/me` |
| GET | `api/Product/Title` | `api/Product/by-title` |
| GET | `api/Product/Condition` | `api/Product/by-condition` |
| GET | `api/Product/MyComments` | `api/Product/comments/me` |
| GET | `api/Product/{productId}/comments` | *(giữ)* `api/Product/{productId}/comments` |
| POST | `api/Product/Post` | `api/Product` |
| POST | `api/Product/Comment` | `api/Product/{productId}/comments` |
| PUT | `api/Product/Post/{id}` | `api/Product/{id}` |
| DELETE | `api/Product/Post/{id}` | `api/Product/{id}` |
| DELETE | `api/Product/Comment/{id}` | `api/Product/comments/{id}` |
| PUT | `api/Product/Comment/{id}` | `api/Product/comments/{id}` |

### Order

| Method | Trước | Sau |
|--------|--------|-----|
| GET | `api/Order/all-orders` | `api/Order` |
| GET | `api/Order/my-order` | `api/Order/me` |
| GET | `api/Order/{id}/status` | *(giữ)* |
| POST | `api/Order/checkout` | *(giữ)* `api/Order/checkout` |
| PUT | `api/Order/{orderId}/confirm-receipt` | *(giữ)* |
| PUT | `api/Order/{orderId}/cancel` | *(giữ)* |
| PUT | `api/Order/{orderId}/cancel-checkout` | *(giữ)* |
| GET | `api/Order/get-products-by-orderId` | `api/Order/{orderId}/products/{productId}` |
| PUT | `api/Order/{orderId}/shipping-address` | *(giữ)* |

### Category

| Method | Trước | Sau |
|--------|--------|-----|
| GET | `api/Category` | *(giữ)* |
| GET | `api/Category/{parentId}/children` | *(giữ)* |
| POST | `api/Category/create` | `api/Category` |
| PUT | `api/Category/update/{categoryId}` | `api/Category/{categoryId}` |
| DELETE | `api/Category/delete/{categoryId}` | `api/Category/{categoryId}` |

### User

| Method | Trước | Sau |
|--------|--------|-----|
| GET | `api/User/MyProfile/{id}` | *(giữ)* |
| GET | `api/User/GetAll` | `api/User` |
| GET | `api/User/GetUserByName` | `api/User/by-username/{userName}` |
| POST | `api/User/Register` | *(giữ)* |
| PUT | `api/User/Profile/{id}` | `api/User/{id}` |
| DELETE | `api/User/{id}` | *(giữ)* |
| POST | `api/User/admin/register-admin` | *(giữ)* |

### Cart

| Method | Trước | Sau |
|--------|--------|-----|
| GET | `api/Cart/my-cart` | `api/Cart/me` |
| POST | `api/Cart/api/add-product-to-cart` | `api/Cart/{userId}/items` |
| DELETE | `api/Cart/api/carts/items/{productId}` | `api/Cart/{userId}/items/{productId}` |

### Dispute

| Method | Trước | Sau |
|--------|--------|-----|
| POST | `api/Dispute/create/{orderId}` | **`api/orders/{orderId}/disputes`** |
| GET | `api/Dispute/my-disputes` | `api/Dispute/me` |
| POST | `api/Dispute/{disputeId}/cancel` | `api/Dispute/{disputeId}/cancellation` |
| GET | `api/Dispute/admin/disputes` | `api/Dispute` |
| PATCH | `api/Dispute/admin/{disputeId}/assign` | `api/Dispute/{disputeId}/assignment` |
| POST | `api/Dispute/admin/{disputeId}/resolve` | `api/Dispute/{disputeId}/resolution` |

### Identity

| Method | Trước | Sau |
|--------|--------|-----|
| POST | `api/Identity/login` | *(giữ)* |

### Report

| Method | Trước | Sau |
|--------|--------|-----|
| POST | `api/Report/CreateReport` | `api/Report` |
| GET | `api/Report/GetReport` | `api/Report` |
| GET | `api/Report/GetReport/{id}` | `api/Report/{id}` |
| PUT | `api/Report/AproveReport/BannedProduct` | `api/Report/{reportId}/approve` |
| PUT | `api/Report/RejectReport` | `api/Report/{reportId}/reject` |

### Promotion

| Method | Trước | Sau |
|--------|--------|-----|
| GET | `api/Promotion/promotion-packages/available` | `api/Promotion/packages/available` |
| GET | `api/Promotion/my-subscriptions` | `api/Promotion/subscriptions/me` |
| GET | `api/Promotion/product-promotions` | *(giữ)* |
| POST | `api/Promotion/admin/promotion-packages` | `api/Promotion/promotion-packages` |
| POST | `api/Promotion/promotion-packages/subscribe/{packageId}` | `api/Promotion/promotion-packages/{packageId}/subscriptions` |
| POST | `api/Promotion/product-promotions/apply` | *(giữ)* |
| PATCH | `api/Promotion/product-promotions/{id}/toggle` | *(giữ)* |

### Shipper

| Method | Trước | Sau |
|--------|--------|-----|
| GET | `api/Shipper/available-orders` | `api/Shipper/orders/available` |
| POST | `api/Shipper/accept-order` | `api/Shipper/{shipperId}/orders/{orderId}/acceptance` |
| GET | `api/Shipper/{shipperId}/my-orders` | `api/Shipper/{shipperId}/orders` |
| GET | `api/Shipper/my-ordersByOrderID` | `api/Shipper/{shipperId}/orders/{orderId}` |
| POST | `api/Shipper/confirm-pickup` | `api/Shipper/{shipperId}/orders/{orderId}/pickup` |
| POST | `api/Shipper/confirm-delivery` | `api/Shipper/{shipperId}/orders/{orderId}/delivery` |

### Wallet

| Method | Trước | Sau |
|--------|--------|-----|
| GET | `api/Wallet/my-wallet` | `api/Wallet/me` |

### Sepay

| Method | Trước | Sau |
|--------|--------|-----|
| POST | `api/Sepay/webhook` | *(giữ)* |
| GET | `api/Sepay/qrcode` | `api/Sepay/qr-code` |

### Notification

| Method | Trước | Sau |
|--------|--------|-----|
| PUT | `api/Notification/MarkAsRead` | `api/Notification/{notificationId}/read` |
| GET | `api/Notification/GetNotifications` | `api/Notification` (+ query `userId`, …) |

### IdentifyDocument

| Method | Trước | Sau |
|--------|--------|-----|
| POST | `api/IdentifyDocument/Submit` | `api/IdentifyDocument` |
| PUT | `api/IdentifyDocument/Re-Submit` | *(giữ)* |
| GET | `api/IdentifyDocument/GetAll/StatusPending` | `api/IdentifyDocument` (+ query `status`, …) |
| GET | `api/IdentifyDocument/GetById` | `api/IdentifyDocument/{documentId}` |
| GET | `api/IdentifyDocument/GetMyDocument` | `api/IdentifyDocument/me` |
| PUT | `api/IdentifyDocument/Approve` | `api/IdentifyDocument/{documentId}/approval` |
| PUT | `api/IdentifyDocument/Reject` | `api/IdentifyDocument/{documentId}/rejection` |

---

## Chi tiết endpoint **hiện tại** (tham chiếu)

## Product (`api/Product`)

| Method | Đường dẫn đầy đủ | Ghi chú |
|--------|------------------|---------|
| GET | `api/Product` | Query: `pageSize`, `pageIndex` |
| GET | `api/Product/me` | Buyer |
| GET | `api/Product/by-title` | Query: `searchTerm`, `pageSize`, `pageIndex` |
| GET | `api/Product/by-condition` | Query: `searchTerm`, `pageSize`, `pageIndex` |
| GET | `api/Product/comments/me` | Buyer |
| GET | `api/Product/{productId}/comments` | |
| POST | `api/Product` | Buyer, `multipart/form-data` (tạo sản phẩm) |
| POST | `api/Product/{productId}/comments` | Buyer, JSON body (`content`, `parentCommentId`; `productId` từ URL) |
| PUT | `api/Product/{id}` | Buyer, `multipart/form-data` |
| DELETE | `api/Product/{id}` | Admin hoặc Seller |
| DELETE | `api/Product/comments/{id}` | Buyer |
| PUT | `api/Product/comments/{id}` | Buyer, JSON |

---

## Order (`api/Order`)

| Method | Đường dẫn đầy đủ | Ghi chú |
|--------|------------------|---------|
| GET | `api/Order` | Admin — danh sách tất cả đơn |
| GET | `api/Order/me` | Buyer |
| GET | `api/Order/{id}/status` | Buyer |
| POST | `api/Order/checkout` | Buyer, JSON body |
| PUT | `api/Order/{orderId}/confirm-receipt` | Buyer |
| PUT | `api/Order/{orderId}/cancel` | Buyer, JSON body |
| PUT | `api/Order/{orderId}/cancel-checkout` | Buyer |
| GET | `api/Order/{orderId}/products/{productId}` | |
| PUT | `api/Order/{orderId}/shipping-address` | Buyer, JSON body |

---

## Category (`api/Category`)

| Method | Đường dẫn đầy đủ |
|--------|------------------|
| GET | `api/Category` |
| GET | `api/Category/{parentId}/children` |
| POST | `api/Category` |
| PUT | `api/Category/{categoryId}` |
| DELETE | `api/Category/{categoryId}` |

---

## User (`api/User`)

| Method | Đường dẫn đầy đủ | Ghi chú |
|--------|------------------|---------|
| GET | `api/User/MyProfile/{id}` | JWT |
| GET | `api/User` | JWT; query: `searchTerm`, `pageIndex`, `pageSize` |
| GET | `api/User/by-username/{userName}` | JWT |
| POST | `api/User/Register` | `multipart/form-data` |
| PUT | `api/User/{id}` | `multipart/form-data` |
| DELETE | `api/User/{id}` | JWT |
| POST | `api/User/admin/register-admin` | Admin, `multipart/form-data` (tạo shipper) |

---

## Cart (`api/Cart`)

| Method | Đường dẫn đầy đủ | Ghi chú |
|--------|------------------|---------|
| GET | `api/Cart/me` | Buyer; query: `pageIndex`, `pageSize` |
| POST | `api/Cart/{userId}/items` | Buyer, body |
| DELETE | `api/Cart/{userId}/items/{productId}` | Buyer |

---

## Dispute (`api/Dispute`) + tạo đơn khiếu nại

| Method | Đường dẫn đầy đủ | Ghi chú |
|--------|------------------|---------|
| POST | **`api/orders/{orderId}/disputes`** | Buyer, JSON (template `~/`, không nằm dưới `api/Dispute`) |
| GET | `api/Dispute/me` | Buyer; query: `pageSize`, `pageIndex` |
| POST | `api/Dispute/{disputeId}/cancellation` | Buyer |
| GET | `api/Dispute` | Admin; query: `status`, `pageSize`, `pageIndex` |
| PATCH | `api/Dispute/{disputeId}/assignment` | Admin, JSON |
| POST | `api/Dispute/{disputeId}/resolution` | Admin, JSON |

---

## Identity (`api/Identity`)

| Method | Đường dẫn đầy đủ |
|--------|------------------|
| POST | `api/Identity/login` |

---

## Report (`api/Report`)

| Method | Đường dẫn đầy đủ | Ghi chú |
|--------|------------------|---------|
| POST | `api/Report` | Buyer |
| GET | `api/Report` | Admin; query: `searchTerm`, `pageSize`, `pageIndex` |
| GET | `api/Report/{id}` | Admin |
| PUT | `api/Report/{reportId}/approve` | Admin |
| PUT | `api/Report/{reportId}/reject` | Admin |

---

## Promotion (`api/Promotion`)

| Method | Đường dẫn đầy đủ |
|--------|------------------|
| GET | `api/Promotion/packages/available` |
| GET | `api/Promotion/subscriptions/me` |
| GET | `api/Promotion/product-promotions` |
| POST | `api/Promotion/promotion-packages` |
| POST | `api/Promotion/promotion-packages/{packageId}/subscriptions` |
| POST | `api/Promotion/product-promotions/apply` |
| PATCH | `api/Promotion/product-promotions/{id}/toggle` |

---

## Shipper (`api/Shipper`)

| Method | Đường dẫn đầy đủ | Ghi chú |
|--------|------------------|---------|
| GET | `api/Shipper/orders/available` | |
| POST | `api/Shipper/{shipperId}/orders/{orderId}/acceptance` | |
| GET | `api/Shipper/{shipperId}/orders` | Query: `pageSize`, `pageIndex` |
| GET | `api/Shipper/{shipperId}/orders/{orderId}` | |
| POST | `api/Shipper/{shipperId}/orders/{orderId}/pickup` | `multipart` (`pod1Image`) |
| POST | `api/Shipper/{shipperId}/orders/{orderId}/delivery` | `multipart` (`pod2Image`) |

---

## Wallet (`api/Wallet`)

| Method | Đường dẫn đầy đủ |
|--------|------------------|
| GET | `api/Wallet/me` |

---

## Sepay (`api/Sepay`)

| Method | Đường dẫn đầy đủ | Ghi chú |
|--------|------------------|---------|
| POST | `api/Sepay/webhook` | AllowAnonymous, JSON |
| GET | `api/Sepay/qr-code` | AllowAnonymous; query: `amount`, `referenceCode` |

---

## Notification (`api/Notification`)

| Method | Đường dẫn đầy đủ | Ghi chú |
|--------|------------------|---------|
| PUT | `api/Notification/{notificationId}/read` | |
| GET | `api/Notification` | Query: `userId`, `pageIndex`, `pageSize` |

---

## IdentifyDocument (`api/IdentifyDocument`)

| Method | Đường dẫn đầy đủ | Ghi chú |
|--------|------------------|---------|
| POST | `api/IdentifyDocument` | Buyer |
| PUT | `api/IdentifyDocument/Re-Submit` | Buyer |
| GET | `api/IdentifyDocument` | Admin; query: `status`, `pageIndex`, `pageSize` |
| GET | `api/IdentifyDocument/{documentId}` | |
| GET | `api/IdentifyDocument/me` | Buyer |
| PUT | `api/IdentifyDocument/{documentId}/approval` | Admin |
| PUT | `api/IdentifyDocument/{documentId}/rejection` | Admin; query: `reason` |

---

## Cập nhật tài liệu

Khi đổi `[Route(...)]` hoặc template action trong controller, **cập nhật lại file này** cho khớp.
