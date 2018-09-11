using Microsoft.AspNetCore.Mvc.Rendering;
using Models;
using System;
using System.Collections.Generic;

namespace ViewModels
{
    public class TonSxDataViewModel : ExtensionViewModel
    {
        public FactoryTonSX Entity;

        public IList<FactoryProduct> Products;
        public IList<Unit> Units;
    }
}
