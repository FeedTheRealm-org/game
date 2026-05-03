# Builds

## CLI build

- Compilation with default unity RAM usage and speed performance.

```bash
~/Unity/Hub/Editor/6000.3.0f1/Editor/Unity \
  -batchmode \
  -nographics \
  -quit \
  -projectPath "<path-to-project>" \
  -executeMethod BuildScript.BuildLinux \
  -logFile build.log
```

- Compilation for low RAM usage but slower performance.

```bash
~/Unity/Hub/Editor/6000.3.0f1/Editor/Unity \
  -batchmode \
  -nographics \
  -quit \
  -job-worker-count 3 \
  -burst-disable-compilation-in-editor \
  -skipShaderCompilation \
  -gc-max-heap-size=1024 \
  -disable-assembly-updater \
  -projectPath "<path-to-project>" \
  -executeMethod BuildScript.BuildLinux \
  -logFile build.log
```
