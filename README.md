# events_api

RESTful API сервис для управления событиями.

## Требования для запуска
* .NET 10.0 SDK (или новее)
* Git

---

## Сборка и запуск проекта через командную строку

### 1. Клонирование репозитория и переход в ветку
Если репозиторий еще не склонирован:
```bash
git clone -b sprint-2 https://github.com/michaelchemmsu-eng/events_api.git
cd events_api
```

### 2. Сборка проекта
В корневой папке репозитория выполните:
```bash
dotnet build
```

### 3. Запуск Тестов
```bash
dotnet test
```
### 4. Запуск проекта

```bash
dotnet run --project project
```

---

## Проверка работы (Swagger)
После запуска откройте в браузере:
* `http://localhost:<порт>/swagger`










## Фильтрация и пагинация событий (`GET /events`)
- Метод получения списка событий поддерживает следующие опциональные параметры строки запроса (Query parameters):
- string title  -- Поиск по названию события
- DateTime from -- Фильтр по дате: события, которые начинаются не раньше указанной даты (StartAt >= from)
- DateTime to -- Фильтр по дате: события, которые заканчиваются не позже указанной даты (EndAt <= to)
- int page -- Номер страницы (по умолчанию: 1)
- int pageSize -- Количество событий на странице (по умолчанию: 10)

примеры запросов:
- GET /api/events?title=тайтл
- GET /api/events?from=2025-06-01T00:00:00Z&to=2025-06-30T23:59:59Z
- GET /api/events?page=2&pageSize=5

## Формат ответа при ошибках
```JSON
{
  "title": "Resourse not found",
  "status": 404,
  "detail": "Событие с идентификатором '3fa85f64-5717-4562-b3fc-2c963f66afa6' не найдено.",
  "instance": "/api/Events/3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```









## Создание бронирования для события c идентициактором eventId (`POST /events/{eventId}/book`)
- Метод создания бронирования поддерживает параметр eventId типа Guid - идентификатор события, для которого создается бронирование.
пример запроса:
- POST /events/3fa85f64-5717-4562-b3fc-2c963f66afa6/book

## формат ответа при ошибках
``` JSON
{
  "title": "Событие не найдено",
  "status": 404,
  "detail": "Событие с Id 3fa85f64-5717-4562-b3fc-2c963f66afa6 не найдено"
}
```
** Response headers **
 `content-type: application/problem+json; charset=utf-8 
 date: Thu,08 Oct 2026 07:07:52 GMT 
 server: Kestrel` 

 ## формат ответа при успешном создании бронирования
 ``` JSON
 {
  "id": "ba45e8c6-d038-4f2a-ac18-3081288cbda1",
  "eventId": "d33ae92d-501f-4bd6-8056-a3bb1f36fd0f",
  "status": "Pending"
}
 ```
** Response headers **
 `content-type: application/json; charset=utf-8 
 date: Thu,08 Oct 2026 07:11:32 GMT 
 location: /bookings/ba45e8c6-d038-4f2a-ac18-3081288cbda1 
 server: Kestrel`









 
## получить событие по id; если не найдено — вернуть корректный HTTP-ответ
* **Метод:** `GET`
* **URL:** `/events/{Id}`
* **Успешный ответ:** `200 OK`
* **Тело ответа:**
```json
{
  "id": "d33ae92d-501f-4bd6-8056-a3bb1f36fd0f",
  "title": "string1",
  "description": "string1",
  "startAt": "2026-09-08T07:11:04.25Z",
  "endAt": "2026-10-08T07:11:04.25Z"
}
```
**Response headers**
`content-type: application/json; charset=utf-8 
 date: Thu,08 Oct 2026 07:15:54 GMT 
 server: Kestrel`

* **ответ в случае отсутствия события:** `404 OK`
* **Тело ответа:**
```json
{
  "title": "Resourse not found",
  "status": 404,
  "detail": "Событие с идентификатором 'd23ae92d-501f-4bd6-8056-a3bb1f36fd0f' не найдено.",
  "instance": "/events/d23ae92d-501f-4bd6-8056-a3bb1f36fd0f"
}
```
**Response headers**
`content-type: application/json; charset=utf-8 
 date: Thu,08 Oct 2026 07:20:06 GMT 
 server: Kestrel `









## POST /events — создать событие, возвращать корректный HTTP-ответ (например, 201)
* **Тело запроса:**
```json
{
  "title": "string2",
  "description": "string2",
  "startAt": "2026-09-08T07:11:04.25Z",
  "endAt": "2026-10-08T07:11:04.25Z"
}
```
* **Проверка валидации входных данных:**
  * title — заголовок события , обязательное поле, не может быть пустым
  * description — описание события ,необязательное поле
  * startAt — дата и время начала события , обязательное поле, должно быть меньше endAt
  * endAt — дата и время окончания события , обязательное поле, должно быть больше startAt
* **Метод:** `POST`
* **URL:** `/events`
* **Успешный ответ:** `201 Created`
* **Тело ответа:**
```json
{
  "id": "1692a748-922f-4dd4-a3a7-04d11b68ca61",
  "title": "string2",
  "description": "string2",
  "startAt": "2026-09-08T07:11:04.25Z",
  "endAt": "2026-10-08T07:11:04.25Z"
}
```
**Response headers**
` content-type: application/json; charset=utf-8 
 date: Thu,08 Oct 2026 07:24:22 GMT 
 location: https://localhost:7087/events/1692a748-922f-4dd4-a3a7-04d11b68ca61 
 server: Kestrel `
* **Неуспешный ответ (валидация):** `400 Bad Request`
```JSON
{
  "title": "Vallidation Error occurred",
  "status": 400,
  "instance": "/events",
  "errors": {
    "EndAt": [
      "Дата начала события должна быть меньше даты окончания события"
    ],
    "StartAt": [
      "Дата начала события должна быть меньше даты окончания события"
    ]
  }
}
```
**Response headers**
 `content-type: application/problem+json; charset=utf-8 
 date: Thu,08 Oct 2026 07:30:39 GMT 
 server: Kestrel`
















## обновить событие целиком; если не найдено — вернуть корректный HTTP-ответ (например, 404)
* **Метод:** `PUT`
* **URL:** `/events/{Id}`
* **Успешный ответ:** `200 OK`
* **Тело ответа:**
```json
{
  "message": "Событие С Id d33ae92d-501f-4bd6-8056-a3bb1f36fd0f успешно обновлено"
}
```
**Response headers**
`content-type: application/json; charset=utf-8 
 date: Thu,08 Oct 2026 07:35:51 GMT 
 server: Kestrel `
* **ответ в случае ошибки - отсутсвие события:** `404 Not Found`
* **Тело ответа:**
```json
{
  "title": "Resourse not found",
  "status": 404,
  "detail": "Событие с идентификатором d32ae92d-501f-4bd6-8056-a3bb1f36fd0f не найдено.",
  "instance": "/events/d32ae92d-501f-4bd6-8056-a3bb1f36fd0f"
}
```
**Response headers**
` content-type: application/json; charset=utf-8 
 date: Thu,08 Oct 2026 07:38:13 GMT 
 server: Kestrel  `













## удалить событие; если не найдено — вернуть корректный HTTP-ответ (например, 404)
* **Метод:** `DELETE`
* **URL:** `/events/{Id}`
* **Успешный ответ:** `200 OK`
* **Тело ответа:**
```json
{
  "message": "Событие С Id d33ae92d-501f-4bd6-8056-a3bb1f36fd0f успешно удалено"
}
```
**Response headers**
`content-type: application/json; charset=utf-8 
 date: Thu,08 Oct 2026 07:40:13 GMT 
 server: Kestrel  `
* **ответ в случае ошибки - отсутсвие события:** `404 Not Found`
* **Тело ответа:**
```json
{
  "title": "Resourse not found",
  "status": 404,
  "detail": "Событие с идентификатором d33ae92d-501f-4bd6-8056-a3bb1f36fd0f не найдено.",
  "instance": "/events/d33ae92d-501f-4bd6-8056-a3bb1f36fd0f"
}
```
**Response headers**
`content-type: application/json; charset=utf-8 
 date: Thu,08 Oct 2026 07:40:51 GMT 
 server: Kestrel  `











 ## возвращает текущее состояние брони по её идентификатору
* **Метод:** `GET`
* **URL:** `/bookings/{bookingId}`
* **Успешный ответ:** `202 OK`
* **Тело ответа:**
```json
{
  "bookingId": "33e5c5f9-af6e-4d05-97a0-f728170bcecb",
  "eventId": "1692a748-922f-4dd4-a3a7-04d11b68ca61",
  "status": 1
}
```
фоновый процесс обновляет статус бронирования в течение 2 секунд, как минимум, после создания, поэтому при повторном запросе через 2 секунды статус бронирования изменится на 2.
* **Тело ответа после обновления фоновым процессом:**
```json
{
  "bookingId": "33e5c5f9-af6e-4d05-97a0-f728170bcecb",
  "eventId": "1692a748-922f-4dd4-a3a7-04d11b68ca61",
  "status": 2
}
```

**Response headers**
`content-type: application/json; charset=utf-8 
 date: Thu,08 Oct 2026 07:47:27 GMT 
 server: Kestrel `
* **ответ в случае ошибки - отсутсвие бронирования:** `404 Not Found`
* **Тело ответа:**
```json
{
  "title": "Бронирование не найдено",
  "status": 404,
  "detail": "Бронирование с Id 33e5c5f9-af6e-4d05-97a0-f728170bcecb не найдено",
  "instance": "/bookings/33e5c5f9-af6e-4d05-97a0-f728170bcecb"
}
```
**Response headers**
`content-type: application/problem+json; charset=utf-8 
 date: Thu,08 Oct 2026 07:50:08 GMT 
 server: Kestrel`






## реализована модель Booking - сущность бронирования события
**Состав полей:**
- Id (Guid, обязательное) — уникальный идентификатор брони;
- EventId (Guid, обязательное) — идентификатор события, к которому относится бронь;
- Status (BookingStatus, обязательное) — текущий статус брони;
- CreatedAt (DateTime, обязательное) — дата и время создания брони;
- ProcessedAt (DateTime?, опциональное) — дата и время обработки брони.


- глобальная обработка ошибок через кастомный Middleware.
- Все ошибки возвращаются в стандартизированном JSON-формате по спецификации Problem Details:
- 400 Bad Request — ошибки валидации входных данных
- 404 Not Found — запрашиваемое событие с указанным id не найдено
- 500 Internal Server Error — непредвиденные сбои на стороне сервера


## текушие статусы бронирования события в enum BookingStatus со значениями:
- Pending = 1  — бронь создана, ожидает обработки;
- Confirmed = 2 — бронь подтверждена;
- Rejected = 3 — бронь отклонена.

## пример сценария использования
запрос на создание бронирования события с Id 317f1592-9109-4050-a6ea-a8ada60dcaf0:
```https://localhost:7087/events/317f1592-9109-4050-a6ea-a8ada60dcaf0/book
запрос на получение статуса бронирования с Id 8e318042-0986-46cd-a641-2d4ec6769b4c:
```https://localhost:7087/bookings/8e318042-0986-46cd-a641-2d4ec6769b4c