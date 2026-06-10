# Система учёта АТП

Курсовая работа: автоматизированная система автотранспортного предприятия.

## Технологии
- C# / WinForms / .NET Framework
- MySQL (Docker)
- Costura.Fody (упаковка в один EXE)

## Как запустить
1. Установите Docker Desktop
2. Запустите: `docker compose up -d`
3. Запустите `ATP.exe` из папки Publish

## Структура БД
- vehicles — автомобили
- drivers — водители
- maintenance — ТО и ремонты
- fuel_logs — заправки
