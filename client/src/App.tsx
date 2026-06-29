import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { HomePage } from "./pages/HomePage";
import { ListDetailPage } from "./pages/ListDetailPage";
import { RecipeDetailPage } from "./pages/RecipeDetailPage";
import { RecipesPage } from "./pages/RecipesPage";
import { ShoppingListsPage } from "./pages/ShoppingListsPage";

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/lists" element={<ShoppingListsPage />} />
        <Route path="/lists/:listId" element={<ListDetailPage />} />
        <Route path="/recipes" element={<RecipesPage />} />
        <Route path="/recipes/:recipeId" element={<RecipeDetailPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
