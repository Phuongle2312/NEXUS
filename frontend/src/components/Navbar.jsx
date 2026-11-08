import React from 'react';
import { Phone, Search, Home, Layers, Tag, MessageSquareText } from 'lucide-react';

export default function Navbar({ activeSection = 'home', onNavigate }) {
  const navItems = [
    { id: 'home', label: 'Trang Chủ', icon: Home },
    { id: 'plans', label: 'Gói Cước & Dịch Vụ', icon: Layers },
    { id: 'schemes', label: 'Ưu Đãi Số Lượng Lớn', icon: Tag },
    { id: 'tracking', label: 'Tra Cứu Đơn & Cước', icon: Search },
    { id: 'feedback', label: 'Góp Ý & Phản Hồi', icon: MessageSquareText }
  ];

  return (
    <header className="main-navbar">
      <div className="navbar-container">
        {/* Brand Logo */}
        <div className="brand-wrapper" onClick={() => onNavigate('home')} style={{ cursor: 'pointer' }}>
          <img
            src="https://coderent.vn/storage/settings/YfEwVsPubmDolpSAhDLDc5DKuDdRCZZccY3M9yfO.png"
            alt="Nexus Telecom - Service Marketing System"
            className="brand-logo-img"
          />
        </div>

        {/* Navigation Items */}
        <nav className="nav-center-menu">
          <ul className="nav-links">
            {navItems.map((item) => {
              const Icon = item.icon;
              const isActive = activeSection === item.id;
              return (
                <li key={item.id}>
                  <button
                    type="button"
                    className={`nav-link-btn ${isActive ? 'active' : ''}`}
                    onClick={() => onNavigate(item.id)}
                  >
                    <Icon size={16} className="nav-item-icon" />
                    <span>{item.label}</span>
                  </button>
                </li>
              );
            })}
          </ul>
        </nav>

        {/* Actions */}
        <div className="header-actions">
          <a href="tel:19008888" className="btn btn-secondary btn-sm" style={{ textDecoration: 'none' }}>
            <Phone size={15} />
            <span className="hotline-text">1900 8888</span>
          </a>
          <button
            type="button"
            className="btn btn-primary btn-sm"
            onClick={() => onNavigate('tracking')}
          >
            <Search size={15} />
            <span>Tra Cứu Nhanh</span>
          </button>
        </div>
      </div>
    </header>
  );
}
