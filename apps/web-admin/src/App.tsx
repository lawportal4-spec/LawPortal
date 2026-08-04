import { BrowserRouter, Routes, Route } from "react-router-dom";
import Login from "./pages/Login";
import Dashboard from "./pages/Dashboard";
import LawyerVerification from "./pages/LawyerVerification";
import Payments from "./pages/Payments";
import PaymentDetail from "./pages/PaymentDetail";
import Ledger from "./pages/Ledger";
import Catalog from "./pages/Catalog";
import Subscriptions from "./pages/Subscriptions";
import Users from "./pages/Users";
import AuditLog from "./pages/AuditLog";
import { AuthProvider } from "./lib/authContext";
import { RequireAuth } from "./components/RequireAuth";

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route path="/" element={<RequireAuth><Dashboard /></RequireAuth>} />
          <Route path="/lawyers" element={<RequireAuth><LawyerVerification /></RequireAuth>} />
          <Route path="/payments" element={<RequireAuth><Payments /></RequireAuth>} />
          <Route path="/payments/:id" element={<RequireAuth><PaymentDetail /></RequireAuth>} />
          <Route path="/ledger" element={<RequireAuth><Ledger /></RequireAuth>} />
          <Route path="/catalog" element={<RequireAuth><Catalog /></RequireAuth>} />
          <Route path="/subscriptions" element={<RequireAuth><Subscriptions /></RequireAuth>} />
          <Route path="/users" element={<RequireAuth><Users /></RequireAuth>} />
          <Route path="/audit" element={<RequireAuth><AuditLog /></RequireAuth>} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
