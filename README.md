# events_api

RESTful API сервис для управления событиями.

## Требования для запуска
* .NET 8.0 SDK (или новее)
* Git

---

## Сборка и запуск проекта через командную строку

### 1. Клонирование репозитория и переход в ветку
Если репозиторий еще не склонирован:
```bash
git clone -b sprint-1 https://github.com/michaelchemmsu-eng/events_api.git
cd events_api
```

### 2. Сборка проекта
В корневой папке репозитория выполните:
```bash
dotnet build
```

### 3. Запуск проекта

```bash
dotnet run
```

---

## Проверка работы (Swagger)
После запуска откройте в браузере:
* `https://localhost:<порт>/swagger`