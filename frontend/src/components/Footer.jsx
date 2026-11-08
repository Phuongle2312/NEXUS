import React from 'react';

export default function Footer() {
  return (
    <footer className="nexus-footer">
      <div className="footer-content">
        <div>
          <div style={{ fontWeight: 800, fontSize: '1.1rem', color: 'var(--secondary)' }}>
            NEXUS COMMUNICATION SYSTEM
          </div>
          <div style={{ marginTop: '4px' }}>
            Hệ thống cung cấp dịch vụ Internet Băng Rộng, Dial-up & Viễn thông cố định.
          </div>
          <div style={{ fontSize: '0.775rem', marginTop: '6px', color: 'var(--text-muted)' }}>
            eProject Aptech 2026 • Frontend: React + JavaScript • UI Standard: Light Theme, Root Radius: 8px
          </div>
        </div>
        <div style={{ textAlign: 'right' }}>
          <div style={{ fontWeight: 600, color: 'var(--text-primary)' }}>
            Đội ngũ phát triển: Nhánh ManhDev
          </div>
          <div style={{ fontSize: '0.8rem', marginTop: '4px' }}>
            Lê Trí Phương • Lữ Văn Giỏi • Nguyễn Trần Xuân Cường
          </div>
          <div style={{ fontSize: '0.775rem', marginTop: '6px', color: 'var(--text-muted)' }}>
            Bản quyền © 2026 Nexus Telecom. Tất cả các quyền được bảo lưu.
          </div>
        </div>
      </div>
    </footer>
  );
}

