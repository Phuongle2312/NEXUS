import React, { useState, useEffect } from 'react';
import TopHeader from './components/TopHeader.jsx';
import Navbar from './components/Navbar.jsx';
import Footer from './components/Footer.jsx';
import PublicView from './views/PublicView.jsx';

export default function App() {
  const [activeSection, setActiveSection] = useState('home');

  const handleNavigation = (section) => {
    setActiveSection(section);

    if (section === 'home') {
      window.scrollTo({ top: 0, behavior: 'smooth' });
      return;
    }

    const sectionMap = {
      plans: 'plans-section',
      schemes: 'schemes-section',
      tracking: 'tracking-section',
      feedback: 'feedback-section'
    };

    const targetId = sectionMap[section];
    if (targetId) {
      const el = document.getElementById(targetId);
      if (el) {
        el.scrollIntoView({ behavior: 'smooth' });
      }
    }
  };

  // ScrollSpy: Tự động cập nhật activeSection khi người dùng cuộn trang
  useEffect(() => {
    let timeoutId;

    const handleScroll = () => {
      // Debounce nhẹ để tối ưu hiệu năng
      clearTimeout(timeoutId);
      timeoutId = setTimeout(() => {
        const scrollPosition = window.scrollY + 160;

        const sections = [
          { id: 'feedback', el: document.getElementById('feedback-section') },
          { id: 'tracking', el: document.getElementById('tracking-section') },
          { id: 'schemes', el: document.getElementById('schemes-section') },
          { id: 'plans', el: document.getElementById('plans-section') },
          { id: 'home', el: document.getElementById('home-section') }
        ];

        for (const section of sections) {
          if (section.el) {
            const top = section.el.offsetTop;
            if (scrollPosition >= top) {
              setActiveSection(section.id);
              break;
            }
          }
        }
      }, 50);
    };

    window.addEventListener('scroll', handleScroll, { passive: true });
    return () => {
      clearTimeout(timeoutId);
      window.removeEventListener('scroll', handleScroll);
    };
  }, []);

  return (
    <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <TopHeader />
      <Navbar activeSection={activeSection} onNavigate={handleNavigation} />

      <main className="app-container">
        <PublicView />
      </main>

      <Footer />
    </div>
  );
}
