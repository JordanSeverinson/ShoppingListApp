using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ShoppingList.Infrastructure.Persistence;

#nullable disable

namespace ShoppingList.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260705120000_RemoveRecipeShareCodeAndAddShareStatus")]
partial class RemoveRecipeShareCodeAndAddShareStatus
{
}
