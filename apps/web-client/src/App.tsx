import { BrowserRouter, Routes, Route } from "react-router-dom";
import PortalHome from "./pages/PortalHome";
import ServicesHub from "./pages/ServicesHub";
import LawyerDirectory from "./pages/LawyerDirectory";
import LawyerProfile from "./pages/LawyerProfile";
import Login from "./pages/Login";
import Orders from "./pages/Orders";
import OrderDetail from "./pages/OrderDetail";
import NewBiddingRequest from "./pages/NewBiddingRequest";
import Chat from "./pages/Chat";
import { AuthProvider } from "./lib/authContext";
import { RequireAuth } from "./components/RequireAuth";

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<PortalHome />} />
          <Route path="/services" element={<ServicesHub />} />
          <Route path="/lawyers" element={<LawyerDirectory />} />
          <Route path="/lawyers/:slug" element={<LawyerProfile />} />
          <Route path="/login" element={<Login />} />
          <Route
            path="/bidding/new"
            element={
              <RequireAuth>
                <NewBiddingRequest />
              </RequireAuth>
            }
          />
          <Route
            path="/orders"
            element={
              <RequireAuth>
                <Orders />
              </RequireAuth>
            }
          />
          <Route
            path="/orders/:id"
            element={
              <RequireAuth>
                <OrderDetail />
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
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
