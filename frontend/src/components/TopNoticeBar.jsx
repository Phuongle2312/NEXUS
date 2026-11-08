import React from 'react';

export default function TopNoticeBar({ currentRole, onRoleChange }) {
  const roleLabels = {
    PUBLIC: 'Khách Vãng Lai',
    RETAIL: 'Nhân Viên Quầy (Lữ Văn Giỏi)',
    TECHNICAL: 'Kỹ Thuật Viên (Nguyễn Trần Xuân Cường)',
    ACCOUNTS: 'Kế Toán Viên (Phạm Thị Mai)',
    ADMIN: 'Quản Trị Viên (Lê Trí Phương)',
    CUSTOMER: 'Khách Hàng (Alpha Corp)'
  };

  return (
    <aside className="top-notice-bar">
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
        <span>💡 Chế độ mô phỏng kiểm thử giao diện React:</span>
        <span style={{ color: '#38bdf8', fontWeight: 700 }}>
          {roleLabels[currentRole] || 'Khách'}
        </span>
      </div>
      <div className="role-switcher-container">
        <label htmlFor="react-role-selector" className="role-tag">
          Chuyển Phân Hệ / Vai Trò:
        </label>
        <select
          id="react-role-selector"
          className="role-selector"
          value={currentRole}
          onChange={(e) => onRoleChange(e.target.value)}
        >
          <option value="PUBLIC">🌐 Khách Vãng Lai (Trang Chủ & Tra Cứu)</option>
          <option value="RETAIL">🏪 Nhân Viên Quầy (Retail Outlet)</option>
          <option value="TECHNICAL">🔧 Nhân Viên Kỹ Thuật (Technical)</option>
          <option value="ACCOUNTS">📊 Bộ Phận Kế Toán (Accounts & Billing)</option>
          <option value="ADMIN">⚙️ Quản Trị Hệ Thống (Administrator)</option>
          <option value="CUSTOMER">👤 Cổng Khách Hàng (Customer Portal)</option>
        </select>
      </div>
    </aside>
  );
}
