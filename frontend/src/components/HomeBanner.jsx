import React, { useState, useEffect } from 'react';
import { Zap, ShieldCheck, ArrowRight, ChevronLeft, ChevronRight, Wifi, PhoneCall, Sparkles, Award } from 'lucide-react';

export default function HomeBanner() {
  const [currentSlide, setCurrentSlide] = useState(0);
  const [isPaused, setIsPaused] = useState(false);

  const slides = [
    {
      id: 1,
      badge: 'CÔNG NGHỆ BĂNG RỘNG 2026',
      badgeColor: '#0284c7',
      title: 'Internet Cáp Quang Nexus Broadband',
      highlightTitle: 'Tốc Độ Vượt Trội, Trải Nghiệm Mượt Mà',
      desc: 'Hạ tầng viễn thông cáp quang băng rộng thế hệ mới, độ trễ cực thấp dưới 1ms, tốc độ đường truyền ổn định không giới hạn dung lượng. Phù hợp cho hộ gia đình và văn phòng làm việc hiện đại.',
      primaryBtnText: 'Khám Phá Các Gói Cước',
      primaryBtnLink: '#plans-section',
      secondaryBtnText: 'Tra Cứu Đơn & Cước',
      secondaryBtnLink: '#tracking-section',
      features: ['Tốc độ cao đến 128Kbps - 1Gbps', 'Bảo mật đường truyền tuyệt đối', 'Hỗ trợ kỹ thuật 24/7'],
      visualType: 'broadband',
      accentColor: '#0284c7'
    },
    {
      id: 2,
      badge: 'CHÍNH SÁCH DOANH NGHIỆP',
      badgeColor: '#16a34a',
      title: 'Ưu Đãi Lắp Đặt Số Lượng Lớn (Corporate Schemes)',
      highlightTitle: 'Chiết Khấu Đến 100% Tiền Cọc & Cước',
      desc: 'Chương trình chiết khấu đặc biệt dành cho doanh nghiệp, trường học, bệnh viện đăng ký từ 10 kết nối trở lên. Giảm trực tiếp 25% - 100% tiền đặt cọc thiết bị và cước ứng trước kỳ đầu.',
      primaryBtnText: 'Tính Thử Chiết Khấu Ngay',
      primaryBtnLink: '#schemes-section',
      secondaryBtnText: 'Bảng Giá Chi Tiết',
      secondaryBtnLink: '#plans-section',
      features: ['Từ 10-15 đường: Giảm 25%', 'Từ 26-50 đường: Giảm 75%', 'Trên 50 đường: Miễn phí 100%'],
      visualType: 'corporate',
      accentColor: '#16a34a'
    },
    {
      id: 3,
      badge: 'VIỄN THÔNG CỐ ĐỊNH & BẢO MẬT',
      badgeColor: '#7c3aed',
      title: 'Điện Thoại Nexus Landline & Dial-up',
      highlightTitle: 'Kết Nối Thông Suốt, Tiết Kiệm Tối Đa',
      desc: 'Mạng thoại cố định nội hạt và liên tỉnh (STD) cước gọi ưu đãi chỉ từ 0.55$/phút. Dịch vụ Internet Dial-up đường truyền riêng ổn định, tương thích hoàn hảo hạ tầng điện thoại bàn Nexus.',
      primaryBtnText: 'Xem Gói Cước Thoại',
      primaryBtnLink: '#plans-section',
      secondaryBtnText: 'Gửi Góp Ý / Khiếu Nại',
      secondaryBtnLink: '#feedback-section',
      features: ['Gói thuê bao năm chỉ 75$', 'Gọi liên tỉnh STD chất lượng cao', 'Miễn phí bảo dưỡng định kỳ'],
      visualType: 'landline',
      accentColor: '#7c3aed'
    }
  ];

  // Auto-play slide
  useEffect(() => {
    if (isPaused) return;
    const timer = setInterval(() => {
      setCurrentSlide((prev) => (prev + 1) % slides.length);
    }, 5500);
    return () => clearInterval(timer);
  }, [isPaused, slides.length]);

  const slide = slides[currentSlide];

  const handlePrev = () => {
    setCurrentSlide((prev) => (prev - 1 + slides.length) % slides.length);
  };

  const handleNext = () => {
    setCurrentSlide((prev) => (prev + 1) % slides.length);
  };

  return (
    <section
      id="home-section"
      className="home-banner-wrapper"
      onMouseEnter={() => setIsPaused(true)}
      onMouseLeave={() => setIsPaused(false)}
    >
      <div className="home-banner-card">
        {/* Left Column: Text & CTA */}
        <div className="home-banner-content">
          <div className="banner-badge" style={{ backgroundColor: slide.badgeColor }}>
            <Sparkles size={13} />
            <span>{slide.badge}</span>
          </div>

          <h1 className="banner-title">
            <span className="banner-title-main">{slide.title}</span>
            <span className="banner-title-sub" style={{ color: slide.accentColor }}>
              {slide.highlightTitle}
            </span>
          </h1>

          <p className="banner-description">{slide.desc}</p>

          {/* Key Feature Bullets */}
          <div className="banner-features-row">
            {slide.features.map((feat, idx) => (
              <div key={idx} className="banner-feature-pill">
                <ShieldCheck size={15} color={slide.accentColor} />
                <span>{feat}</span>
              </div>
            ))}
          </div>

          {/* Action Buttons */}
          <div className="banner-actions">
            <a href={slide.primaryBtnLink} className="btn btn-primary btn-lg">
              <span>{slide.primaryBtnText}</span>
              <ArrowRight size={17} />
            </a>
            <a href={slide.secondaryBtnLink} className="btn btn-secondary btn-lg">
              <span>{slide.secondaryBtnText}</span>
            </a>
          </div>
        </div>

        {/* Right Column: Visual Infographic / Live Stats Card */}
        <div className="home-banner-visual">
          {slide.visualType === 'broadband' && (
            <div className="visual-graphic-box visual-broadband">
              <div className="visual-main-icon">
                <Wifi size={48} color="#0284c7" />
              </div>
              <div className="visual-metric-title">Tốc Độ Cáp Quang Băng Rộng</div>
              <div className="visual-metric-value">
                128 <span>Kbps / 1Gbps</span>
              </div>
              <div className="visual-badge-pill">Độ trễ Ping: &lt; 1ms</div>

              <div className="visual-stats-row">
                <div className="v-stat-item">
                  <strong>99.9%</strong>
                  <span>Uptime</span>
                </div>
                <div className="v-stat-item">
                  <strong>IPv6</strong>
                  <span>Hỗ trợ sẵn</span>
                </div>
                <div className="v-stat-item">
                  <strong>$500</strong>
                  <span>Cọc hoàn lại</span>
                </div>
              </div>
            </div>
          )}

          {slide.visualType === 'corporate' && (
            <div className="visual-graphic-box visual-corporate">
              <div className="visual-main-icon" style={{ background: '#dcfce7', color: '#16a34a' }}>
                <Award size={48} color="#16a34a" />
              </div>
              <div className="visual-metric-title">Mức Giảm Giá Tối Đa</div>
              <div className="visual-metric-value" style={{ color: '#16a34a' }}>
                100% <span>Miễn Phí</span>
              </div>
              <div className="visual-badge-pill" style={{ background: '#dcfce7', color: '#166534' }}>
                Áp dụng cho đơn &gt; 50 kết nối
              </div>

              <div className="visual-stats-row">
                <div className="v-stat-item">
                  <strong>10–15</strong>
                  <span>Giảm 25%</span>
                </div>
                <div className="v-stat-item">
                  <strong>16–25</strong>
                  <span>Giảm 50%</span>
                </div>
                <div className="v-stat-item">
                  <strong>26–50</strong>
                  <span>Giảm 75%</span>
                </div>
              </div>
            </div>
          )}

          {slide.visualType === 'landline' && (
            <div className="visual-graphic-box visual-landline">
              <div className="visual-main-icon" style={{ background: '#f3e8ff', color: '#7c3aed' }}>
                <PhoneCall size={48} color="#7c3aed" />
              </div>
              <div className="visual-metric-title">Cước Thoại Cố Định Nexus</div>
              <div className="visual-metric-value" style={{ color: '#7c3aed' }}>
                0.55$ <span>/ phút</span>
              </div>
              <div className="visual-badge-pill" style={{ background: '#f3e8ff', color: '#6b21a8' }}>
                Gói năm chỉ 75$ thuê bao
              </div>

              <div className="visual-stats-row">
                <div className="v-stat-item">
                  <strong>Nội hạt</strong>
                  <span>0.55$ / p</span>
                </div>
                <div className="v-stat-item">
                  <strong>STD</strong>
                  <span>1.75$ / p</span>
                </div>
                <div className="v-stat-item">
                  <strong>SMS</strong>
                  <span>1.00$ / tin</span>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Slide Navigation Controls */}
      <button className="banner-nav-arrow banner-arrow-left" onClick={handlePrev} aria-label="Slide trước">
        <ChevronLeft size={20} />
      </button>
      <button className="banner-nav-arrow banner-arrow-right" onClick={handleNext} aria-label="Slide sau">
        <ChevronRight size={20} />
      </button>

      {/* Dots Indicator */}
      <div className="banner-dots-indicator">
        {slides.map((_, idx) => (
          <button
            key={idx}
            className={`banner-dot ${currentSlide === idx ? 'active' : ''}`}
            onClick={() => setCurrentSlide(idx)}
            aria-label={`Chuyển đến slide ${idx + 1}`}
          />
        ))}
      </div>
    </section>
  );
}

