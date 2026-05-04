# OCR GitHub Star Research

This repo-local script checks candidate OCR engines through the GitHub API and ranks by stargazers.

| Rank | Repository | Stars | URL |
|---:|---|---:|---|
| 1 | PaddlePaddle/PaddleOCR | 77011 | https://github.com/PaddlePaddle/PaddleOCR |
| 2 | tesseract-ocr/tesseract | 73865 | https://github.com/tesseract-ocr/tesseract |
| 3 | ocrmypdf/OCRmyPDF | 33523 | https://github.com/ocrmypdf/OCRmyPDF |
| 4 | JaidedAI/EasyOCR | 29396 | https://github.com/JaidedAI/EasyOCR |
| 5 | mindee/doctr | 6058 | https://github.com/mindee/doctr |

## Selected Local OCR

Selected: `PaddlePaddle/PaddleOCR` because it has the highest GitHub stars among the configured local OCR candidates.

Note: the scene generator invokes `tesseract` locally. If the executable is missing, the markdown report records that OCR could not run.