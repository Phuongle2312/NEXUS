import React from 'react';
import { Phone, PlusCircle } from 'lucide-react';

export default function Navbar({ currentRole, onRoleChange }) {
  const isPublic = currentRole === 'PUBLIC';

  return (
    <header className="main-navbar">
      <div className="navbar-container">
        <div className="brand-wrapper" onClick={() => onRoleChange('PUBLIC')} style={{ cursor: 'pointer' }}>
          <img
            src="https://coderent.vn/storage/settings/YfEwVsPubmDolpSAhDLDc5DKuDdRCZZccY3M9yfO.png"
            alt="Nexus Telecom - Service Marketing System"
            className="brand-logo-img"
          />
        </div>

        <nav>
          <ul className="nav-links">
            {isPublic ? (
              <>
                <li>
                  <button className="nav-link-btn active" onClick={() => onRoleChange('PUBLIC')}>
                    Trang Chủ
                  </button>
                </li>
                <li>
                  <a href="#plans-section" className="nav-link-btn" style={{ textDecoration: 'none' }}>
                    Gói Cước
                  </a>
                </li>
                <li>
                  <a href="#tracking-section" className="nav-link-btn" style={{ textDecoration: 'none' }}>
                    Tra Cứu Đơn & Tài Khoản
                  </a>
                </li>
                <li>
                  <button className="nav-link-btn" onClick={() => onRoleChange('RETAIL')}>
                    Điểm Giao Dịch
                  </button>
                </li>
              </>
            ) : (
              <>
                <li>
                  <button
                    className={`nav-link-btn ${currentRole === 'PUBLIC' ? 'active' : ''}`}
                    onClick={() => onRoleChange('PUBLIC')}
                  >
                    🌐 Trang Chủ
                  </button>
                </li>
                <li>
                  <button
                    className={`nav-link-btn ${currentRole === 'RETAIL' ? 'active' : ''}`}
                    onClick={() => onRoleChange('RETAIL')}
                  >
                    🏪 Quầy Bán Lẻ
                  </button>
                </li>
                <li>
                  <button
                    className={`nav-link-btn ${currentRole === 'TECHNICAL' ? 'active' : ''}`}
                    onClick={() => onRoleChange('TECHNICAL')}
                  >
                    🔧 Kỹ Thuật
                  </button>
                </li>
                <li>
                  <button
                    className={`nav-link-btn ${currentRole === 'ACCOUNTS' ? 'active' : ''}`}
                    onClick={() => onRoleChange('ACCOUNTS')}
                  >
                    📊 Kế Toán & Cước
                  </button>
                </li>
                <li>
                  <button
                    className={`nav-link-btn ${currentRole === 'ADMIN' ? 'active' : ''}`}
                    onClick={() => onRoleChange('ADMIN')}
                  >
                    ⚙️ Quản Trị
                  </button>
                </li>
                <li>
                  <button
                    className={`nav-link-btn ${currentRole === 'CUSTOMER' ? 'active' : ''}`}
                    onClick={() => onRoleChange('CUSTOMER')}
                  >
                    👤 Khách Hàng
                  </button>
                </li>
              </>
            )}
          </ul>
        </nav>

        <div className="header-actions">
          <a href="tel:19008888" className="btn btn-secondary btn-sm" style={{ textDecoration: 'none' }}>
            <Phone size={15} />
            Hotline: 1900 8888
          </a>
          <button className="btn btn-primary btn-sm" onClick={() => onRoleChange('RETAIL')}>
            <PlusCircle size={15} />
            Đăng Ký Đặt Đơn
          </button>
        </div>
      </div>
    </header>
  );
}
