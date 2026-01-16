// Copyright 2025 Code Philosophy
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using Luban.Datas;
using Luban.Defs;
using Luban.Types;
using Luban.Validator;

namespace Luban.DataValidator.Builtin.Type;

[Validator("constructor")]
public class ConstructorValidator : DataValidatorBase
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();

    private DefBean _baseBean;
    private List<string> _validTypeNames;
    private Func<DType, string> _stringGetter;

    public override void Compile(DefField field, TType type)
    {
        string baseBeanName = Args.Trim();
        if (string.IsNullOrEmpty(baseBeanName))
        {
            throw new Exception($"field:{field} constructor 验证器参数不能为空");
        }

        var assembly = field.Assembly;
        DefBean baseBean;

        // 尝试直接查找
        var baseType = assembly.GetDefType(baseBeanName);

        // 如果找不到，尝试在当前模块中查找
        if (baseType == null)
        {
            var currentModule = field.HostType.Namespace;
            baseType = assembly.GetDefType(currentModule, baseBeanName);
        }

        if (baseType == null)
        {
            throw new Exception($"field:{field} constructor 基类 '{baseBeanName}' 不存在");
        }

        if (baseType is not DefBean)
        {
            throw new Exception($"field:{field} constructor '{baseBeanName}' 不是 Bean 类型");
        }

        baseBean = (DefBean)baseType;
        _baseBean = baseBean;

        // 收集所有有效类型名称（基类及其所有子类）
        _validTypeNames = _baseBean.GetHierarchyChildren()
            .Select(b => b.Name)
            .ToList();

        switch (type)
        {
            case TString:
            {
                _stringGetter = d => ((DString)d).Value;
                break;
            }
            default:
            {
                throw new Exception($"field:{field} constructor 验证器只支持 string 类型，当前类型:{type.TypeName}");
            }
        }
    }

    public override void Validate(DataValidatorContext ctx, TType type, DType data)
    {
        string beanName = _stringGetter(data);

        if (string.IsNullOrWhiteSpace(beanName))
        {
            s_logger.Error($"记录 {RecordPath} (来自文件:{Source}) constructor 值不能为空");
            GenerationContext.Current.LogValidatorFail(this);
            return;
        }

        var assembly = GenerationContext.Current.Assembly;
        var targetType = assembly.GetDefType(beanName);

        // 如果找不到，尝试在基类所在的模块中查找
        if (targetType == null)
        {
            targetType = assembly.GetDefType(_baseBean.Namespace, beanName);
        }

        if (targetType == null)
        {
            s_logger.Error($"记录 {RecordPath} = '{beanName}' (来自文件:{Source}) 类型不存在。有效类型: [{string.Join(", ", _validTypeNames)}]");
            GenerationContext.Current.LogValidatorFail(this);
            return;
        }

        if (targetType is not DefBean targetBean)
        {
            s_logger.Error($"记录 {RecordPath} = '{beanName}' (来自文件:{Source}) 不是 Bean 类型。有效类型: [{string.Join(", ", _validTypeNames)}]");
            GenerationContext.Current.LogValidatorFail(this);
            return;
        }

        if (!_baseBean.IsAssignableFrom(targetBean))
        {
            s_logger.Error($"记录 {RecordPath} = '{beanName}' (来自文件:{Source}) 不是基类 '{_baseBean.FullName}' 或其子类。有效类型: [{string.Join(", ", _validTypeNames)}]");
            GenerationContext.Current.LogValidatorFail(this);
        }
    }
}
