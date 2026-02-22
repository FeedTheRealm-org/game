using Cysharp.Threading.Tasks;
using FTRShared.Runtime.Models;

namespace FTR.Core.Common.Loaders;

public interface ILoader
{
    UniTask<WorldData> Load();
}
