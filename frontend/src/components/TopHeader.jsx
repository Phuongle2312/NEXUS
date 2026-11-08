import React, { useState } from 'react';
import { Phone, Mail, MapPin, Clock, Globe, Sparkles, ChevronDown } from 'lucide-react';

export default function TopHeader() {
  const [lang, setLang] = useState('VI');

  return (
    <div className="top-header-bar">
      <div className="top-header-container">
        {/* Left Side: Contact & Stores */}
        <div className="top-header-left">
          <a href="tel:19008888" className="top-header-item">
            <Phone size={13} className="top-item-icon text-sky" />
            <span>Hotline 24/7: <strong>1900 8888</strong></span>
          </a>

          <span className="top-header-divider">•</span>

          <a href="mailto:support@nexus.com" className="top-header-item">
            <Mail size={13} className="top-item-icon text-sky" />
            <span>support@nexus.com</span>
          </a>

          <span className="top-header-divider">•</span>

          <div className="top-header-item store-item">
            <MapPin size={13} className="top-item-icon text-emerald" />
            <span>Mạng lưới 64+ điểm giao dịch toàn quốc</span>
          </div>
        </div>

        {/* Right Side: Promo Notice & Language */}
        <div className="top-header-right">
          <div className="top-promo-pill">
            <Sparkles size={12} className="promo-sparkle" />
            <span>Ưu đãi doanh nghiệp: Giảm đến 100% cọc & cước</span>
          </div>

          <span className="top-header-divider">•</span>

          <div className="top-header-item">
            <Clock size={13} className="top-item-icon" />
            <span>07:30 – 21:30 (Cả lễ & CN)</span>
          </div>

          <span className="top-header-divider">•</span>

          {/* Language Switcher */}
          <div className="top-lang-dropdown">
            <button
              type="button"
              className="lang-btn"
              onClick={() => setLang(lang === 'VI' ? 'EN' : 'VI')}
              title="Chuyển đổi ngôn ngữ"
            >
              <Globe size={13} />
              <span>{lang === 'VI' ? 'Tiếng Việt' : 'English'}</span>
              <ChevronDown size={11} />
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

