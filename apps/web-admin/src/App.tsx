import { BrowserRouter, Routes, Route } from "react-router-dom";
import Login from "./pages/Login";
import Dashboard from "./pages/Dashboard";
import LawyerRegistrations from "./pages/LawyerRegistrations";
import LawyerRegistrationDetail from "./pages/LawyerRegistrationDetail";
import Payments from "./pages/Payments";
import PaymentDetail from "./pages/PaymentDetail";
import Ledger from "./pages/Ledger";
import Catalog from "./pages/Catalog";
import Subscriptions from "./pages/Subscriptions";
import Users from "./pages/Users";
import AuditLog from "./pages/AuditLog";
import DiscountCodes from "./pages/DiscountCodes";
import DiscountCodeNew from "./pages/DiscountCodeNew";
import DiscountCodeEdit from "./pages/DiscountCodeEdit";
import Account from "./pages/Account";
import LawyerDebts from "./pages/LawyerDebts";
import LawyerDebt from "./pages/LawyerDebt";
import Settings from "./pages/Settings";
import Clients from "./pages/Clients";
import Client from "./pages/Client";
import Requests from "./pages/Requests";
import Request from "./pages/Request";
import { AuthProvider } from "./lib/authContext";
import { RequireAuth } from "./components/RequireAuth";

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route path="/" element={<RequireAuth><Dashboard /></RequireAuth>} />
          <Route path="/lawyers" element={<RequireAuth><LawyerRegistrations /></RequireAuth>} />
          <Route path="/lawyers/:id" element={<RequireAuth><LawyerRegistrationDetail /></RequireAuth>} />
          <Route path="/payments" element={<RequireAuth><Payments /></RequireAuth>} />
          <Route path="/payments/:id" element={<RequireAuth><PaymentDetail /></RequireAuth>} />
          <Route path="/ledger" element={<RequireAuth><Ledger /></RequireAuth>} />
          <Route path="/catalog" element={<RequireAuth><Catalog /></RequireAuth>} />
          <Route path="/subscriptions" element={<RequireAuth><Subscriptions /></RequireAuth>} />
          <Route path="/discount-codes" element={<RequireAuth><DiscountCodes /></RequireAuth>} />
          <Route path="/discount-codes/new" element={<RequireAuth><DiscountCodeNew /></RequireAuth>} />
          <Route path="/discount-codes/:id" element={<RequireAuth><DiscountCodeEdit /></RequireAuth>} />
          <Route path="/users" element={<RequireAuth><Users /></RequireAuth>} />
          <Route path="/lawyer-debts" element={<RequireAuth><LawyerDebts /></RequireAuth>} />
          <Route path="/lawyer-debts/:id" element={<RequireAuth><LawyerDebt /></RequireAuth>} />
          <Route path="/clients" element={<RequireAuth><Clients /></RequireAuth>} />
          <Route path="/clients/:id" element={<RequireAuth><Client /></RequireAuth>} />
          <Route path="/requests" element={<RequireAuth><Requests /></RequireAuth>} />
          <Route path="/requests/:id" element={<RequireAuth><Request /></RequireAuth>} />
          <Route path="/settings" element={<RequireAuth><Settings /></RequireAuth>} />
          <Route path="/account" element={<RequireAuth><Account /></RequireAuth>} />
          <Route path="/audit" element={<RequireAuth><AuditLog /></RequireAuth>} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
