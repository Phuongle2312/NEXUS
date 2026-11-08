import React, { useState } from 'react';
import { EMPLOYEES, SHOPS, CITIES, PLANS_DATA } from '../data/mockData.js';
import { Users, Building, MapPin, Package, Plus } from 'lucide-react';

export default function AdminView() {
  const [activeTab, setActiveTab] = useState('EMPLOYEES');
  const [employees, setEmployees] = useState(EMPLOYEES);
  const [shops, setShops] = useState(SHOPS);

  const handleAddEmployee = () => {
    const username = prompt('Nhập username cho nhân viên mới:');
    if (username) {
      setEmployees([
        ...employees,
        {
          id: employees.length + 1,
          username,
          fullName: 'Nhân viên mới tuyển',
          role: 'RETAIL',
          shopName: 'Nexus Store Ba Đình',
          email: `${username}@nexus.com`
        }
      ]);
    }
  };

  const handleAddShop = () => {
    const name = prompt('Nhập tên điểm giao dịch mới:');
    if (name) {
      setShops([
        ...shops,
        {
          id: shops.length + 1,
          name,
          cityCode: '001',
          address: 'Hà Nội',
          phone: '024.9999.8888'
        }
      ]);
    }
  };

  return (
    <div>
      <div className="flex-between" style={{ marginBottom: '1.5rem' }}>
        <div>
          <h2 style={{ fontSize: '1.5rem', fontWeight: 800, color: 'var(--text-primary)' }}>
            ⚙️ Quản Trị Hệ Thống (Admin Control Center)
          </h2>
          <p className="form-hint">Quản trị danh mục hệ thống, nhân sự, chi nhánh bán lẻ và định cấu hình dịch vụ</p>
        </div>
      </div>

      {/* Tabs */}
      <div className="service-nav-tabs">
        <button
          className={`service-tab-btn ${activeTab === 'EMPLOYEES' ? 'active' : ''}`}
          onClick={() => setActiveTab('EMPLOYEES')}
        >
          <Users size={16} style={{ display: 'inline', marginRight: '6px' }} />
          Nhân Viên ({employees.length})
        </button>
        <button
          className={`service-tab-btn ${activeTab === 'SHOPS' ? 'active' : ''}`}
          onClick={() => setActiveTab('SHOPS')}
        >
          <Building size={16} style={{ display: 'inline', marginRight: '6px' }} />
          Cửa Hàng Bán Lẻ ({shops.length})
        </button>
        <button
          className={`service-tab-btn ${activeTab === 'CITIES' ? 'active' : ''}`}
          onClick={() => setActiveTab('CITIES')}
        >
          <MapPin size={16} style={{ display: 'inline', marginRight: '6px' }} />
          Mã Thành Phố ({CITIES.length})
        </button>
        <button
          className={`service-tab-btn ${activeTab === 'PLANS' ? 'active' : ''}`}
          onClick={() => setActiveTab('PLANS')}
        >
          <Package size={16} style={{ display: 'inline', marginRight: '6px' }} />
          Danh Mục Gói Cước ({PLANS_DATA.length})
        </button>
      </div>

      <div className="nexus-card">
        {activeTab === 'EMPLOYEES' && (
          <div>
            <div className="nexus-card-header">
              <h3 className="nexus-card-title">Quản Lý Nhân Viên & Phân Quyền Vai Trò</h3>
              <button className="btn btn-primary btn-sm" onClick={handleAddEmployee}>
                <Plus size={14} /> Thêm Nhân Viên
              </button>
            </div>
            <div className="table-responsive">
              <table className="nexus-table">
                <thead>
                  <tr>
                    <th>Tài Khoản</th>
                    <th>Họ Và Tên</th>
                    <th>Vai Trò (Role)</th>
                    <th>Đơn Vị Công Tác</th>
                    <th>Email Liên Hệ</th>
                    <th>Trạng Thái</th>
                  </tr>
                </thead>
                <tbody>
                  {employees.map(e => (
                    <tr key={e.id}>
                      <td><code className="font-mono"><strong>{e.username}</strong></code></td>
                      <td><strong>{e.fullName}</strong></td>
                      <td>
                        <span className={`badge ${
                          e.role === 'ADMIN' ? 'badge-danger' :
                          e.role === 'RETAIL' ? 'badge-info' :
                          e.role === 'TECH' ? 'badge-warning' :
                          e.role === 'ACCOUNTS' ? 'badge-success' : 'badge-neutral'
                        }`}>
                          {e.role}
                        </span>
                      </td>
                      <td>{e.shopName}</td>
                      <td>{e.email}</td>
                      <td><span className="badge badge-success">Đang Hoạt Động</span></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {activeTab === 'SHOPS' && (
          <div>
            <div className="nexus-card-header">
              <h3 className="nexus-card-title">Mạng Lưới Điểm Giao Dịch & Cửa Hàng Bán Lẻ</h3>
              <button className="btn btn-primary btn-sm" onClick={handleAddShop}>
                <Plus size={14} /> Mở Điểm Giao Dịch
              </button>
            </div>
            <div className="table-responsive">
              <table className="nexus-table">
                <thead>
                  <tr>
                    <th>Tên Cửa Hàng</th>
                    <th>Mã Thành Phố</th>
                    <th>Địa Chỉ Trực Tiếp</th>
                    <th>Hotline</th>
                    <th>Trạng Thái</th>
                  </tr>
                </thead>
                <tbody>
                  {shops.map(s => (
                    <tr key={s.id}>
                      <td><strong>{s.name}</strong></td>
                      <td><span className="badge badge-neutral">{s.cityCode}</span></td>
                      <td>{s.address}</td>
                      <td>{s.phone}</td>
                      <td><span className="badge badge-success">Mở Cửa</span></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {activeTab === 'CITIES' && (
          <div>
            <div className="nexus-card-header">
              <h3 className="nexus-card-title">Danh Mục Mã Thành Phố (Sinh Account ID)</h3>
              <p className="form-hint">Mã 3 chữ số dùng làm thành phần định danh trong Account ID của khách hàng</p>
            </div>
            <div className="table-responsive">
              <table className="nexus-table">
                <thead>
                  <tr>
                    <th>Mã TP (3 số)</th>
                    <th>Tên Tỉnh / Thành Phố</th>
                    <th>Tiêu Chuẩn Đấu Nối</th>
                    <th>Ghi Chú</th>
                  </tr>
                </thead>
                <tbody>
                  {CITIES.map(c => (
                    <tr key={c.code}>
                      <td><strong className="font-mono" style={{ fontSize: '1.1rem', color: 'var(--primary)' }}>{c.code}</strong></td>
                      <td><strong>{c.name}</strong></td>
                      <td>Đạt chuẩn cáp quang & trạm tổng đài</td>
                      <td>Hạ tầng Nexus hoàn thiện</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {activeTab === 'PLANS' && (
          <div>
            <div className="nexus-card-header">
              <h3 className="nexus-card-title">Quản Lý Danh Mục Gói Cước Toàn Hệ Thống</h3>
            </div>
            <div className="table-responsive">
              <table className="nexus-table">
                <thead>
                  <tr>
                    <th>Mã Gói</th>
                    <th>Tên Gói Cước</th>
                    <th>Loại Dịch Vụ</th>
                    <th>Tốc Độ / Hạn Mức</th>
                    <th>Giá Niêm Yết ($)</th>
                    <th>Tiền Cọc ($)</th>
                    <th>Chu Kỳ</th>
                  </tr>
                </thead>
                <tbody>
                  {PLANS_DATA.map(p => (
                    <tr key={p.id}>
                      <td><code className="font-mono"><strong>{p.code}</strong></code></td>
                      <td><strong>{p.name}</strong></td>
                      <td><span className="badge badge-info">{p.service}</span></td>
                      <td>{p.quota}</td>
                      <td><strong>${p.price}</strong></td>
                      <td>${p.deposit}</td>
                      <td>{p.validity}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
