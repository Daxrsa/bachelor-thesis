# Files Plugin

This plugin owns file management, primarily images.

Capabilities:

- Upload image files with content-type and size validation.
- List uploaded images.
- Return metadata for a single image.
- Delete stored images.
- Serve uploaded files through static URLs.

The plugin exposes static files under `/static/images` by default.

Example upload request:

```bash
curl -X POST http://localhost:8080/files/images \
  -F "file=@/path/to/image.jpg"
```
