import React, { useState } from 'react';
import TopNoticeBar from './components/TopNoticeBar.jsx';
import Navbar from './components/Navbar.jsx';
import Footer from './components/Footer.jsx';

import PublicView from './views/PublicView.jsx';
import RetailView from './views/RetailView.jsx';
import TechnicalView from './views/TechnicalView.jsx';
import AccountsView from './views/AccountsView.jsx';
import AdminView from './views/AdminView.jsx';
import CustomerView from './views/CustomerView.jsx';

import { INITIAL_ORDERS, INITIAL_CONNECTIONS, INITIAL_BILLS } from './data/mockData.js';

export default function App() {
  const [currentRole, setCurrentRole] = useState('PUBLIC');
  const [orders, setOrders] = useState(INITIAL_ORDERS);
  const [connections, setConnections] = useState(INITIAL_CONNECTIONS);
  const [bills, setBills] = useState(INITIAL_BILLS);

  // Retail handlers
  const handleAddOrder = (newOrder) => {
    setOrders([newOrder, ...orders]);
  };

  // Technical handlers
  const handleUpdateOrderStatus = (orderId, newStatus) => {
    setOrders(orders.map(o => o.orderId === orderId ? { ...o, status: newStatus } : o));
  };

  const handleAddConnection = (newConn) => {
    setConnections([newConn, ...connections]);
  };

  const handleUpdateConnectionStatus = (accountId, newStatus) => {
    setConnections(connections.map(c => c.accountId === accountId ? { ...c, status: newStatus } : c));
  };

  const handleReplaceDevice = (accountId, newSerial) => {
    setConnections(connections.map(c => c.accountId === accountId ? { ...c, equipmentSerial: newSerial } : c));
  };

  // Accounts handlers
  const handleAddBill = (newBill) => {
    setBills([newBill, ...bills]);
  };

  const handleCollectPayment = (billId) => {
    setBills(bills.map(b => {
      if (b.billId === billId) {
        return {
          ...b,
          paidAmount: b.totalThisCycle + b.previousDue,
          dueBalance: 0,
          status: 'PAID'
        };
      }
      return b;
    }));
  };

  const handleRegisterFromPlan = (plan) => {
    setCurrentRole('RETAIL');
  };

  return (
    <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <TopNoticeBar currentRole={currentRole} onRoleChange={setCurrentRole} />
      <Navbar currentRole={currentRole} onRoleChange={setCurrentRole} />

      <main className="app-container">
        {currentRole === 'PUBLIC' && (
          <PublicView
            orders={orders}
            connections={connections}
            onRegisterClick={handleRegisterFromPlan}
          />
        )}
        {currentRole === 'RETAIL' && (
          <RetailView
            orders={orders}
            onAddOrder={handleAddOrder}
          />
        )}
        {currentRole === 'TECHNICAL' && (
          <TechnicalView
            orders={orders}
            connections={connections}
            onUpdateOrderStatus={handleUpdateOrderStatus}
            onAddConnection={handleAddConnection}
            onUpdateConnectionStatus={handleUpdateConnectionStatus}
            onReplaceDevice={handleReplaceDevice}
          />
        )}
        {currentRole === 'ACCOUNTS' && (
          <AccountsView
            bills={bills}
            connections={connections}
            onAddBill={handleAddBill}
            onCollectPayment={handleCollectPayment}
          />
        )}
        {currentRole === 'ADMIN' && (
          <AdminView />
        )}
        {currentRole === 'CUSTOMER' && (
          <CustomerView
            connections={connections}
            bills={bills}
          />
        )}
      </main>

      <Footer />
    </div>
  );
}

