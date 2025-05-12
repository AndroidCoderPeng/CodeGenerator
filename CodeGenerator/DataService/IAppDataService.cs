using System.Collections.Generic;
using CodeGenerator.Models;

namespace CodeGenerator.DataService
{
    public interface IAppDataService
    {
        List<DefaultSuffix> GetCodeTypeList();
    }
}