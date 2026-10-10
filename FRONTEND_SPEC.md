# 🎨 FRONTEND TECHNICAL SPECIFICATION (REACT.JS)
> **Dự án:** Web App Đặt phòng Homestay Tự Check-in (Saigon Stays)  
> **Framework:** React.js (Vite) | Mobile-First UI  
> **Authentication:** `@react-oauth/google` + Zustand Store  

---

## 1. TECH STACK & INSTALLATION

- **Framework:** React.js (Vite)
- **Styling:** TailwindCSS + Shadcn/ui (Thiết kế Mobile-First)
- **State Management:** TanStack React Query (`@tanstack/react-query`) + Zustand
- **Icons:** Lucide-React (`lucide-react`)
- **Authentication:** `@react-oauth/google`
- **HTTP Client:** Axios (Hỗ trợ Request/Response Interceptors đính kèm JWT)

### Cài đặt thư viện:
```bash
npm install @react-oauth/google axios zustand @tanstack/react-query lucide-react clsx tailwind-merge
```

---

## 2. GOOGLE OAUTH 2.0 & AUTHENTICATION FLOW

### A. Wrap Provider (`src/main.jsx`)
```javascript
import React from 'react';
import ReactDOM from 'react-dom/client';
import { GoogleOAuthProvider } from '@react-oauth/google';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import App from './App';
import './index.css';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
});

const GOOGLE_CLIENT_ID = import.meta.env.VITE_GOOGLE_CLIENT_ID || 'YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com';

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <GoogleOAuthProvider clientId={GOOGLE_CLIENT_ID}>
      <QueryClientProvider client={queryClient}>
        <App />
      </QueryClientProvider>
    </GoogleOAuthProvider>
  </React.StrictMode>
);
```

### B. Zustand Store Quản lý Auth (`src/stores/useAuthStore.js`)
```javascript
import { create } from 'zustand';
import { persist } from 'zustand/middleware';

export const useAuthStore = create(
  persist(
    (set) => ({
      user: null,
      accessToken: null,
      isAuthenticated: false,
      setAuth: (user, accessToken) =>
        set({
          user,
          accessToken,
          isAuthenticated: true,
        }),
      logout: () =>
        set({
          user: null,
          accessToken: null,
          isAuthenticated: false,
        }),
    }),
    {
      name: 'saigon-stays-auth',
    }
  )
);
```

### C. Axios Client với Interceptor (`src/services/api.js`)
```javascript
import axios from 'axios';
import { useAuthStore } from '../stores/useAuthStore';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api/v1',
  headers: {
    'Content-Type': 'application/json',
  },
});

// Gắn JWT token vào Header cho các request cần xác thực
api.interceptors.request.use((config) => {
  const token = useAuthStore.getState().accessToken;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export default api;
```

### D. Login Modal Component (`src/components/LoginModal.jsx`)
```javascript
import { GoogleLogin } from '@react-oauth/google';
import api from '../services/api';
import { useAuthStore } from '../stores/useAuthStore';

export const LoginModal = ({ isOpen, onClose }) => {
  const setAuth = useAuthStore((state) => state.setAuth);

  if (!isOpen) return null;

  const handleGoogleSuccess = async (credentialResponse) => {
    try {
      const res = await api.post('/auth/google-login', {
        idToken: credentialResponse.credential,
      });

      setAuth(res.data.user, res.data.accessToken);
      onClose();
    } catch (error) {
      console.error('Đăng nhập thất bại:', error);
      alert('Đăng nhập thất bại, vui lòng thử lại!');
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="relative w-full max-w-sm rounded-2xl bg-white p-6 shadow-xl text-center">
        <h3 className="text-xl font-bold text-gray-800 mb-2">Đăng nhập Saigon Stays</h3>
        <p className="text-sm text-gray-500 mb-6">
          Đăng nhập để tự động lưu thông tin và nhận mã phòng nhanh chóng.
        </p>
        <div className="flex justify-center">
          <GoogleLogin
            onSuccess={handleGoogleSuccess}
            onError={() => console.log('Login Failed')}
            useOneTap
          />
        </div>
        <button
          onClick={onClose}
          className="mt-6 text-sm text-gray-400 hover:text-gray-600 underline"
        >
          Đóng
        </button>
      </div>
    </div>
  );
};
```

---

## 3. LUỒNG MÀN HÌNH UI/UX (MOBILE-FIRST)

```
┌────────────────────────────────────────────────────────┐
│                   📱 SAIGON STAYS APP                  │
└────────────────────────────────────────────────────────┘
                           │
                           ▼
  ┌──────────────────────────────────────────────────┐
  │ Màn hình 1: Trang chủ & Chọn Slot                │
  │ • Chọn Chi nhánh (Dropdown)                      │
  │ • Chọn Ngày đặt phòng (Calendar DatePicker)       │
  │ • Danh sách Phòng & Slot (Khung giờ + Giá)       │
  └────────────────────────┬─────────────────────────┘
                           │ Chọn Room + Slot
                           ▼
  ┌──────────────────────────────────────────────────┐
  │ Màn hình 2: Điền thông tin Đặt phòng             │
  │ • Tự động fill Tên + Email nếu đã Login          │
  │ • Nhập Số điện thoại liên hệ                     │
  │ • Nút [Thanh toán ngay] ──> Gọi POST /bookings   │
  └────────────────────────┬─────────────────────────┘
                           │ Nhận BookingId & QR
                           ▼
  ┌──────────────────────────────────────────────────┐
  │ Màn hình 3: Thanh toán VietQR & Auto Polling     │
  │ • Hiển thị ảnh VietQR + Đếm ngược 5:00 phút      │
  │ • Polling API: GET /payments/{bookingId}/status  │
  │   mỗi 3 giây kiểm tra gạch nợ thành công         │
  └────────────────────────┬─────────────────────────┘
                           │ Status == 'PAID'
                           ▼
  ┌──────────────────────────────────────────────────┐
  │ Màn hình 4: Thành công (Success Check-in Info)    │
  │ • Thông báo thanh toán thành công                │
  │ • Mã khóa cửa (Door Passcode) + Wi-Fi & Password │
  │ • Gửi email xác nhận kèm hướng dẫn tự check-in   │
  └──────────────────────────────────────────────────┘
```

---

### 📱 Màn hình 1: Trang chủ (Home)
- **Header:** 
  - Logo Saigon Stays
  - Avatar User (khi đã login) hoặc Nút Đăng nhập Google
- **Chi nhánh (Branch Selector):**
  - Dropdown chọn chi nhánh (Nguyễn Bá Học, D9, D11,...)
  - API: `GET /api/v1/branches`
- **Bộ chọn ngày (Calendar Picker):**
  - Mặc định ngày hôm nay, cho phép chọn ngày tiếp theo.
  - API: `GET /api/v1/availability/calendar?branchId={id}&date={YYYY-MM-DD}`
- **Danh sách phòng & Khung giờ (Rooms & Pricing Slots):**
  - Hiển thị ảnh phòng, tên phòng, trạng thái phòng (`AVAILABLE`).
  - Danh sách chip khung giờ (VD: `11:30 - 14:30`, `18:00 - 22:00`, `Qua đêm`).
  - Giá tương ứng của từng khung giờ.
  - Nhấp chọn slot $\rightarrow$ Chuyển sang Màn hình 2 kèm thông tin phòng & slot đã chọn.

---

### 📱 Màn hình 2: Điền thông tin (Booking Form)
- **Tóm tắt đơn đặt phòng:**
  - Tên phòng, chi nhánh, ngày đặt, khung giờ và tổng tiền.
- **Form nhập thông tin khách:**
  - **Họ và tên:** Tự động điền nếu đã login Google, hoặc cho phép khách vãng lai nhập.
  - **Email:** Tự động điền nếu đã login Google (dùng để nhận mã cửa và Wi-Fi).
  - **Số điện thoại:** Bắt buộc nhập (để liên hệ khẩn cấp).
- **Thao tác:**
  - Nút **"Thanh toán ngay"** $\rightarrow$ Gọi API `POST /api/v1/bookings`
  - Payload gửi lên:
    ```json
    {
      "roomId": "guid-room-id",
      "pricingSlotId": "guid-slot-id",
      "guestName": "Nguyen Van A",
      "guestPhone": "0901234567",
      "guestEmail": "nguyenvana@gmail.com",
      "bookingDate": "2026-10-10"
    }
    ```
  - Phản hồi nhận về: `{ "bookingId": "BK_9981", "totalPrice": 250000, "qrUrl": "https://img.vietqr.io/..." }`
  - Chuyển hướng sang Màn hình 3 (`/payment/BK_9981`).

---

### 📱 Màn hình 3: Thanh toán VietQR & Auto Polling
- **Giao diện:**
  - Ảnh mã VietQR động chứa đúng số tiền và nội dung chuyển khoản (`BK_9981`).
  - Bộ đếm ngược thời gian giữ chỗ (Countdown 5:00 phút).
  - Thông tin chuyển khoản sao chép nhanh (Số TK, Ngân hàng, Nội dung CK).
- **Cơ chế Polling tự động:**
  - Dùng React Query / `useEffect` gọi API kiểm tra trạng thái mỗi 3 giây:
    `GET /api/v1/payments/{bookingId}/status`
  - Trạng thái trả về:
    - `"PENDING"`: Tiếp tục hiển thị đếm ngược và poll.
    - `"PAID"`: Ngừng polling, phát hiệu ứng thành công và tự động redirect sang Màn hình 4.
    - `"CANCELLED"` hoặc hết 5 phút: Hiển thị thông báo hết hạn đặt phòng.

```javascript
// Hook kiểm tra trạng thái thanh toán (src/hooks/usePaymentPolling.js)
import { useQuery } from '@tanstack/react-query';
import api from '../services/api';

export const usePaymentPolling = (bookingId, isEnabled = true) => {
  return useQuery({
    queryKey: ['paymentStatus', bookingId],
    queryFn: async () => {
      const { data } = await api.get(`/payments/${bookingId}/status`);
      return data; // { status: "PAID" | "PENDING" | "CANCELLED" }
    },
    refetchInterval: (query) => {
      if (query.state.data?.status === 'PAID') return false;
      return 3000; // Poll mỗi 3s khi đang PENDING
    },
    enabled: !!bookingId && isEnabled,
  });
};
```

---

### 📱 Màn hình 4: Thành công (Success Check-in)
- **Giao diện:**
  - Icon tích xanh động thông báo: **"Đặt phòng & Thanh toán thành công!"**
  - Thông báo: *"Mã khóa cửa và thông tin Wi-Fi đã được tự động gửi về Email: **{user_email}**"*
- **Thông tin Check-in trực tiếp trên App:**
  - **Mã đơn:** `BK_9981`
  - **Phòng:** Room 101 - Chi nhánh Nguyễn Bá Học
  - **Khung giờ:** 11:30 - 14:30 (10/10/2026)
  - **Mật mã cửa (Door Passcode):** `*123456#`
  - **Wi-Fi:** `SaigonStays_101` / Mật khẩu: `stayhappy2026`
- **Nút hành động:**
  - **"Về trang chủ"**
  - **"Liên hệ Hotline Hỗ trợ"**

---

## 4. BIẾN MÔI TRƯỜNG (`.env`)

```env
VITE_API_BASE_URL=http://localhost:5000/api/v1
VITE_GOOGLE_CLIENT_ID=YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com
```

---

## 5. CẤU TRÚC THƯ MỤC DỰ ÁN FRONTEND (ĐỀ XUẤT)

```
frontend/
├── public/
├── src/
│   ├── assets/
│   ├── components/
│   │   ├── Header.jsx
│   │   ├── LoginModal.jsx
│   │   ├── RoomCard.jsx
│   │   ├── SlotPicker.jsx
│   │   └── VietQRModal.jsx
│   ├── hooks/
│   │   ├── useBranches.js
│   │   ├── useRooms.js
│   │   └── usePaymentPolling.js
│   ├── pages/
│   │   ├── HomePage.jsx
│   │   ├── BookingPage.jsx
│   │   ├── PaymentPage.jsx
│   │   └── SuccessPage.jsx
│   ├── services/
│   │   └── api.js
│   ├── stores/
│   │   └── useAuthStore.js
│   ├── App.jsx
│   ├── main.jsx
│   └── index.css
├── .env
├── package.json
├── tailwind.config.js
└── vite.config.js
```
