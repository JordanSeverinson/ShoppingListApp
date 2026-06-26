import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { LandingPage } from "./pages/LandingPage";
import { ListDetailPage } from "./pages/ListDetailPage";

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<LandingPage />} />
        <Route path="/lists/:listId" element={<ListDetailPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
