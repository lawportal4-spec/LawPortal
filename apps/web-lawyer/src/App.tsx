import { BrowserRouter, Routes, Route } from "react-router-dom";
import Login from "./pages/Login";
import Register from "./pages/Register";
import ForgotPassword from "./pages/ForgotPassword";
import Dashboard from "./pages/Dashboard";
import Requests from "./pages/Requests";
import RequestDetail from "./pages/RequestDetail";
import BiddingFeed from "./pages/BiddingFeed";
import BiddingDetail from "./pages/BiddingDetail";
import BiddingAwarded from "./pages/BiddingAwarded";
import Chat from "./pages/Chat";
import Settings from "./pages/Settings";
import Earnings from "./pages/Earnings";
import Subscription from "./pages/Subscription";
import VerifyEmail from "./pages/VerifyEmail";
import RegistrationFee from "./pages/RegistrationFee";
import Account from "./pages/Account";
import { AuthProvider } from "./lib/authContext";
import { RequireAuth } from "./components/RequireAuth";

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<Register />} />
          <Route path="/forgot-password" element={<ForgotPassword />} />
          <Route path="/verify-email" element={<VerifyEmail />} />
          <Route
            path="/account"
            element={
              <RequireAuth>
                <Account />
              </RequireAuth>
            }
          />
          <Route
            path="/registration-fee"
            element={
              <RequireAuth>
                <RegistrationFee />
              </RequireAuth>
            }
          />
          <Route
            path="/"
            element={
              <RequireAuth>
                <Dashboard />
              </RequireAuth>
            }
          />
          <Route
            path="/requests"
            element={
              <RequireAuth>
                <Requests />
              </RequireAuth>
            }
          />
          <Route
            path="/requests/:id"
            element={
              <RequireAuth>
                <RequestDetail />
              </RequireAuth>
            }
          />
          <Route
            path="/bidding"
            element={
              <RequireAuth>
                <BiddingFeed />
              </RequireAuth>
            }
          />
          <Route
            path="/bidding/awarded"
            element={
              <RequireAuth>
                <BiddingAwarded />
              </RequireAuth>
            }
          />
          <Route
            path="/bidding/:id"
            element={
              <RequireAuth>
                <BiddingDetail />
              </RequireAuth>
            }
          />
          <Route
            path="/chat/:requestId"
            element={
              <RequireAuth>
                <Chat />
              </RequireAuth>
            }
          />
          <Route
            path="/settings"
            element={
              <RequireAuth>
                <Settings />
              </RequireAuth>
            }
          />
          <Route
            path="/earnings"
            element={
              <RequireAuth>
                <Earnings />
              </RequireAuth>
            }
          />
          <Route
            path="/subscription"
            element={
              <RequireAuth>
                <Subscription />
              </RequireAuth>
            }
          />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
