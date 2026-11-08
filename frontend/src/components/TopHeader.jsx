import React from 'react';
import { Phone, Mail, Clock } from 'lucide-react';

export default function TopHeader() {
  return (
    <div className="top-header-bar">
      <div className="top-header-container">
        {/* Left Side: Contact */}
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
        </div>

        {/* Right Side: Working Hours */}
        <div className="top-header-right">
          <div className="top-header-item">
            <Clock size={13} className="top-item-icon text-emerald" />
            <span>Giờ hỗ trợ: <strong>07:30 – 21:30</strong> (Tất cả các ngày)</span>
          </div>
        </div>
      </div>
    </div>
  );
}
