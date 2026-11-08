/* ==========================================================================
   NEXUS MOCK DATA & REPOSITORY (JavaScript)
   ========================================================================== */

export const TAX_RATE = 0.1224; // 12.24% thuế dịch vụ viễn thông nhà nước

export const DEPOSIT_RATES = {
  DIALUP: 325,
  BROADBAND: 500,
  LANDLINE: 250
};

export const BULK_DISCOUNT_TIERS = [
  { min: 1, max: 9, percent: 0, label: "0% (Dưới 10 kết nối)" },
  { min: 10, max: 15, percent: 25, label: "25% Giảm giá" },
  { min: 16, max: 25, percent: 50, label: "50% Giảm giá" },
  { min: 26, max: 50, percent: 75, label: "75% Giảm giá" },
  { min: 51, max: 9999, percent: 100, label: "100% Miễn phí cọc & cước trả trước" }
];

export function getDiscountPercent(quantity) {
  const tier = BULK_DISCOUNT_TIERS.find(t => quantity >= t.min && quantity <= t.max);
  return tier ? tier.percent : 0;
}

export const PLANS_DATA = [
  // Dial-up
  { id: 1, code: 'D-HR-10', name: 'Dial-up Hourly 10h', service: 'DIALUP', type: 'HOURLY', speed: '56 Kbps', price: 50, deposit: 325, validity: '1 Tháng', quota: '10 Giờ', featured: false },
  { id: 2, code: 'D-HR-30', name: 'Dial-up Hourly 30h', service: 'DIALUP', type: 'HOURLY', speed: '56 Kbps', price: 130, deposit: 325, validity: '3 Tháng', quota: '30 Giờ', featured: false },
  { id: 3, code: 'D-HR-60', name: 'Dial-up Hourly 60h', service: 'DIALUP', type: 'HOURLY', speed: '56 Kbps', price: 260, deposit: 325, validity: '6 Tháng', quota: '60 Giờ', featured: false },
  { id: 4, code: 'D-UNL-28M', name: 'Dial-up Unlimited 28K (Tháng)', service: 'DIALUP', type: 'UNLIMITED', speed: '28 Kbps', price: 75, deposit: 325, validity: '1 Tháng', quota: 'Không giới hạn', featured: false },
  { id: 5, code: 'D-UNL-28Q', name: 'Dial-up Unlimited 28K (Quý)', service: 'DIALUP', type: 'UNLIMITED', speed: '150', deposit: 325, validity: '3 Tháng', quota: 'Không giới hạn', featured: false },
  { id: 6, code: 'D-UNL-56M', name: 'Dial-up Unlimited 56K (Tháng)', service: 'DIALUP', type: 'UNLIMITED', speed: '56 Kbps', price: 100, deposit: 325, validity: '1 Tháng', quota: 'Không giới hạn', featured: true },
  { id: 7, code: 'D-UNL-56Q', name: 'Dial-up Unlimited 56K (Quý)', service: 'DIALUP', type: 'UNLIMITED', speed: '56 Kbps', price: 180, deposit: 325, validity: '3 Tháng', quota: 'Không giới hạn', featured: false },

  // Broadband
  { id: 8, code: 'B-HR-30', name: 'Broadband Hourly 30h', service: 'BROADBAND', type: 'HOURLY', speed: '1 Mbps', price: 175, deposit: 500, validity: '1 Tháng', quota: '30 Giờ', featured: false },
  { id: 9, code: 'B-HR-60', name: 'Broadband Hourly 60h', service: 'BROADBAND', type: 'HOURLY', speed: '1 Mbps', price: 315, deposit: 500, validity: '6 Tháng', quota: '60 Giờ', featured: false },
  { id: 10, code: 'B-UNL-64M', name: 'Broadband 64Kbps (Tháng)', service: 'BROADBAND', type: 'UNLIMITED', speed: '64 Kbps', price: 225, deposit: 500, validity: '1 Tháng', quota: 'Không giới hạn', featured: false },
  { id: 11, code: 'B-UNL-64Q', name: 'Broadband 64Kbps (Quý)', service: 'BROADBAND', type: 'UNLIMITED', speed: '400', deposit: 500, validity: '3 Tháng', quota: 'Không giới hạn', featured: false },
  { id: 12, code: 'B-UNL-128M', name: 'Broadband 128Kbps (Tháng)', service: 'BROADBAND', type: 'UNLIMITED', speed: '128 Kbps', price: 350, deposit: 500, validity: '1 Tháng', quota: 'Không giới hạn', featured: true },
  { id: 13, code: 'B-UNL-128Q', name: 'Broadband 128Kbps (Quý)', service: 'BROADBAND', type: 'UNLIMITED', speed: '128 Kbps', price: 445, deposit: 500, validity: '3 Tháng', quota: 'Không giới hạn', featured: false },

  // Landline
  { id: 14, code: 'T-LOC-Y', name: 'Landline Local Unlimited', service: 'LANDLINE', type: 'RENTAL', speed: 'Thoại cố định', price: 75, deposit: 250, validity: '1 Năm', quota: 'Cước gọi 0.55$/phút', featured: false },
  { id: 15, code: 'T-LOC-M', name: 'Landline Local Monthly', service: 'LANDLINE', type: 'RENTAL', speed: 'Thoại cố định', price: 35, deposit: 250, validity: '1 Tháng', quota: 'Cước gọi 0.75$/phút', featured: true },
  { id: 16, code: 'T-STD-M', name: 'Landline STD Monthly', service: 'LANDLINE', type: 'RENTAL', speed: 'Thoại liên tỉnh', price: 125, deposit: 250, validity: '1 Tháng', quota: 'Local 0.70$ / STD 2.25$ / SMS 1.00$', featured: false },
  { id: 17, code: 'T-STD-H', name: 'Landline STD Half-Yearly', service: 'LANDLINE', type: 'RENTAL', speed: 'Thoại liên tỉnh', price: 420, deposit: 250, validity: '6 Tháng', quota: 'Local 0.60$ / STD 2.00$ / SMS 1.15$', featured: false }
];

export const INITIAL_ORDERS = [
  {
    orderId: 'B0000000001',
    customerName: 'Công ty Cổ phần Công nghệ Alpha',
    phone: '0901234567',
    service: 'BROADBAND',
    planName: 'Broadband 128Kbps (Tháng)',
    planPrice: 350,
    quantity: 12,
    discountPercent: 25,
    depositPerConn: 500,
    totalDeposit: 4500,
    totalAdvance: 3150,
    status: 'CONNECTED',
    city: 'Hà Nội (001)',
    shop: 'Nexus Store Ba Đình',
    createdAt: '2026-11-02'
  },
  {
    orderId: 'D0000000002',
    customerName: 'Trường Đại học Thăng Long',
    phone: '0987654321',
    service: 'DIALUP',
    planName: 'Dial-up Unlimited 56K (Tháng)',
    planPrice: 100,
    quantity: 20,
    discountPercent: 50,
    depositPerConn: 325,
    totalDeposit: 3250,
    totalAdvance: 1000,
    status: 'FEASIBLE',
    city: 'Hồ Chí Minh (002)',
    shop: 'Nexus Store Quận 1',
    createdAt: '2026-11-04'
  },
  {
    orderId: 'T0000000003',
    customerName: 'Bệnh viện Đa khoa Tâm Anh',
    phone: '0912348888',
    service: 'LANDLINE',
    planName: 'Landline STD Monthly',
    planPrice: 125,
    quantity: 5,
    discountPercent: 0,
    depositPerConn: 250,
    totalDeposit: 1250,
    totalAdvance: 625,
    status: 'SUBMITTED',
    city: 'Đà Nẵng (003)',
    shop: 'Nexus Store Hải Châu',
    createdAt: '2026-11-07'
  }
];

export const INITIAL_CONNECTIONS = [
  {
    accountId: 'B001000000000001',
    orderId: 'B0000000001',
    customerName: 'Công ty Cổ phần Công nghệ Alpha',
    service: 'BROADBAND',
    planName: 'Broadband 128Kbps (Tháng)',
    status: 'ACTIVE',
    equipmentSerial: 'RTR-CISCO-8801',
    cityCode: '001',
    installationAddress: 'Tầng 8, Tháp A, Keangnam, Cầu Giấy, Hà Nội',
    activatedAt: '2026-11-03'
  },
  {
    accountId: 'B001000000000002',
    orderId: 'B0000000001',
    customerName: 'Công ty Cổ phần Công nghệ Alpha',
    service: 'BROADBAND',
    planName: 'Broadband 128Kbps (Tháng)',
    status: 'ACTIVE',
    equipmentSerial: 'RTR-CISCO-8802',
    cityCode: '001',
    installationAddress: 'Tầng 9, Tháp A, Keangnam, Cầu Giấy, Hà Nội',
    activatedAt: '2026-11-03'
  },
  {
    accountId: 'D002000000000001',
    orderId: 'D0000000002',
    customerName: 'Trường Đại học Thăng Long',
    service: 'DIALUP',
    planName: 'Dial-up Unlimited 56K (Tháng)',
    status: 'TEMPORARILY_INACTIVE',
    equipmentSerial: 'MDM-D-LINK-3301',
    cityCode: '002',
    installationAddress: 'Phòng Lab 2, Tòa Nhà A, Q.1, TP.HCM',
    activatedAt: '2026-10-15'
  }
];

export const INITIAL_BILLS = [
  {
    billId: 'INV-202611-001',
    accountId: 'B001000000000001',
    customerName: 'Công ty Cổ phần Công nghệ Alpha',
    billingCycle: '10/2026 - 11/2026',
    planFee: 350.00,
    usageOverdue: 45.00,
    replacementCharge: 0.00,
    taxableAmount: 395.00,
    taxAmount: 48.35,
    depositAdjusted: 0.00,
    totalThisCycle: 443.35,
    previousDue: 0.00,
    paidAmount: 443.35,
    dueBalance: 0.00,
    status: 'PAID',
    issuedDate: '2026-11-01'
  },
  {
    billId: 'INV-202611-002',
    accountId: 'D002000000000001',
    customerName: 'Trường Đại học Thăng Long',
    billingCycle: '10/2026 - 11/2026',
    planFee: 100.00,
    usageOverdue: 25.50,
    replacementCharge: 40.00,
    taxableAmount: 165.50,
    taxAmount: 20.26,
    depositAdjusted: 0.00,
    totalThisCycle: 185.76,
    previousDue: 120.00,
    paidAmount: 0.00,
    dueBalance: 305.76,
    status: 'OVERDUE',
    issuedDate: '2026-11-01'
  }
];

export const CITIES = [
  { code: '001', name: 'Hà Nội' },
  { code: '002', name: 'TP. Hồ Chí Minh' },
  { code: '003', name: 'Đà Nẵng' },
  { code: '004', name: 'Hải Phòng' },
  { code: '005', name: 'Cần Thơ' }
];

export const SHOPS = [
  { id: 1, name: 'Nexus Store Ba Đình', cityCode: '001', address: '12 Kim Mã, Ba Đình, Hà Nội', phone: '024.3888.1111' },
  { id: 2, name: 'Nexus Store Cầu Giấy', cityCode: '001', address: '88 Cầu Giấy, Hà Nội', phone: '024.3888.2222' },
  { id: 3, name: 'Nexus Store Quận 1', cityCode: '002', address: '45 Lê Duẩn, Q.1, TP.HCM', phone: '028.3999.3333' },
  { id: 4, name: 'Nexus Store Hải Châu', cityCode: '003', address: '102 Bạch Đằng, Đà Nẵng', phone: '0236.3777.4444' }
];

export const EMPLOYEES = [
  { id: 1, username: 'admin', fullName: 'Lê Trí Phương', role: 'ADMIN', shopName: 'Trụ sở chính', email: 'phuong.le@nexus.com' },
  { id: 2, username: 'retail', fullName: 'Lữ Văn Giỏi', role: 'RETAIL', shopName: 'Nexus Store Ba Đình', email: 'gioi.lu@nexus.com' },
  { id: 3, username: 'tech', fullName: 'Nguyễn Trần Xuân Cường', role: 'TECH', shopName: 'Trung tâm Hạ tầng Hà Nội', email: 'cuong.nguyen@nexus.com' },
  { id: 4, username: 'accounts', fullName: 'Phạm Thị Mai', role: 'ACCOUNTS', shopName: 'Ban Tài chính Kế toán', email: 'mai.pham@nexus.com' },
  { id: 5, username: 'customer1', fullName: 'Đặng Tuấn Anh (Alpha Corp)', role: 'CUSTOMER', shopName: 'Online', email: 'alpha.corp@gmail.com' }
];
