import { BrowserRouter, Navigate, Route, Routes } from "react-router";
import { AuthProvider } from "./context/AuthContext";
import { RequireAuth } from "./components/RequireAuth";
import { FriendsPage } from "./pages/FriendsPage";
import { ForgotPasswordPage } from "./pages/ForgotPasswordPage";
import { HomePage } from "./pages/HomePage";
import { ResetPasswordPage } from "./pages/ResetPasswordPage";
import { ListDetailPage } from "./pages/ListDetailPage";
import { LoginPage } from "./pages/LoginPage";
import { RegisterPage } from "./pages/RegisterPage";
import { VerifyEmailPage } from "./pages/VerifyEmailPage";
import { ProfilePage } from "./pages/ProfilePage";
import { RecipeDetailPage } from "./pages/RecipeDetailPage";
import { RecipesPage } from "./pages/RecipesPage";
import { ShoppingListsPage } from "./pages/ShoppingListsPage";

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/verify-email" element={<VerifyEmailPage />} />
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
          <Route path="/reset-password" element={<ResetPasswordPage />} />
          <Route path="/profile" element={<RequireAuth><ProfilePage /></RequireAuth>} />
          <Route path="/friends" element={<RequireAuth><FriendsPage /></RequireAuth>} />
          <Route path="/lists" element={<RequireAuth><ShoppingListsPage /></RequireAuth>} />
          <Route path="/lists/:listId" element={<RequireAuth><ListDetailPage /></RequireAuth>} />
          <Route path="/recipes" element={<RequireAuth><RecipesPage /></RequireAuth>} />
          <Route path="/recipes/:recipeId/edit" element={<RequireAuth><RecipeDetailPage /></RequireAuth>} />
          <Route path="/recipes/:recipeId" element={<RequireAuth><RecipeDetailPage readOnly /></RequireAuth>} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}
