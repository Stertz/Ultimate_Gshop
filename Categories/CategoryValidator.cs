using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ultimate_Gshop.Categories
{
    public static class CategoryValidator
    {
        public static void Validate(Category category)
        {

            ArgumentNullException.ThrowIfNull(category);

            if (category.Title.Length > 32) throw new InvalidOperationException("Máximo 32 caracteres");

            if (category.SubCategories.Count > 11) throw new InvalidOperationException("Máximo 11 sub-categorias");

        }  

    }
}
