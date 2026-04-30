# TestingTestAdapter

TestingTestAdapter 是主要用于适配内部 testing 测试框架的一个 Visual Studio Test Adapter，目标源为 `.exe` 文件。



## Testing

默认情况下，它可发现由 testing 框架所编写的测试用例，但只要可执行文件满足以下条件，就可被发现：

- 生成 `.pdb`

- 导出一个 `testing_adapter_indicator_v1` 函数符号（用作标识）

- 支持 `--list-details` 命令，打印单个可执行文件包含的所有测试用例，输出以下格式的 JSON

  ```json
  {
    "case1": [
      {"file": "C:\\Project\\foo\\test-main.c"},
      {"line": "48"},
      {"suite": "namespace"},
      {"category": "class"},
      {"arg": "case1"},
      {"env": "MyVar1=MyValue1"},
      {"env": "MyVar2=MyValue2"}
    ],
    "case2": [
      {"file": "C:\\Project\\foo\\test-main.c"},
      {"line": "58"},
      {"arg": "case2 -- arg1 arg2 arg3"},
      {"xfail": "true"}
    ]
  }
  ```

- 测试输出结果最好采用 TAP 协议



## Meson Test

如果上述条件匹配失败，会尝试在解决方案根目录适配 meson 构建环境，但支持有限（目前只适配 exitcode 协议的测试用例）。



## Wrapper

通过在 `.runsettings` 中指定 wrapper，可支持其它类型的测试框架。

存在两种 wrapper：

- Discoverer wrapper：TestingTestAdapter 会以参数形式将 source 传递给 wrapper，后者将发现结果以与 `--list-details` 命令相同格式输出即可
- Reporter wrapper：TestingTestAdapter 会将测试执行的结果（包括 exitcode, stdout, stderr ）以 JSON 形式从 stdin 流入 wrapper 中，同时会将解决方案的根目录记录到 `SOLUTION_DIRECTORY` 环境变量中。 Reporter wrapper 负责解析测试结果并以一定格式从 stdout 反馈给 adapter



## .runsettings 例子

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
    <TestingTestAdapterSettings>
        <Discoverer>
            <UseWrapper>true</UseWrapper>
            <Wrapper>.\Wrapper\discover-wrapper.py</Wrapper>
        </Discoverer>
        <Reporter>
            <UseWrapper>true</UseWrapper>
            <Wrapper>.\Wrapper\reporter-wrapper.py</Wrapper>
        </Reporter>
        <WatchdogDisabled>false</WatchdogDisabled>
        <WorkingDirectory>c:\workingDir</WorkingDirectory>
        <PathExtension>c:\bingoDir;c:\fooDir;c:\barDir</PathExtension>
        <EnvironmentVariables>
            <Variable Name="MyVar1" Value="MyValue1" />
            <Variable Name="MyVar2" Value="MyValue2" />
            <Variable Name="PATH" Value="c:\myPathDir" />
        </EnvironmentVariables>
    </TestingTestAdapterSettings>
</RunSettings>
```



