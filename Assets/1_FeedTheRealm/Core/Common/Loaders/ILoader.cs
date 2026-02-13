using Cysharp.Threading.Tasks;
using Models;

namespace FTR.Core.Common.Loaders;

public interface ILoader
{
    UniTask<WorldData> Load();
}
