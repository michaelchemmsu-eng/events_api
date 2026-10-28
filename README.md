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
git clone -b sprint-5 https://github.com/michaelchemmsu-eng/events_api.git
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
- int page -- Номер страницы (по умолчанию: 1), фильтрация по значению: page должен быть больше нуля
- int pageSize -- Количество событий на странице (по умолчанию: 10),  фильтрация по значению: pageSize должен лежать в диапазоне [1,100]

примеры запросов:
- GET /api/events?title=тайтл
- GET /api/events?from=2025-06-01T00:00:00Z&to=2025-06-30T23:59:59Z
- GET /api/events?page=2&pageSize=5

## Формат ответа запроса при стандартных значениях параметров
```JSON
{
  "totalEvents": 0,
  "eventArray": [],
  "currentPage": 1,
  "pageSizeOfCurrentPage": 0
}
```












## Создание бронирования для события c идентициактором eventId (`POST /events/{eventId}/book`)
- Метод создания бронирования поддерживает параметр eventId типа Guid - идентификатор события, для которого создается бронирование. возвращает 202 Accepted;  
- теле ответа возвращает информацию о созданной брони (включая Id, EventId, Status);
- в заголовке Location возвращает ссылку на ресурс брони (например, /bookings/{bookingId});
пример запроса:
- POST /events/3fa85f64-5717-4562-b3fc-2c963f66afa6/book
### формат ответа при ошибке "события не найдено" (KeyNotFoundException)
``` JSON
{
  "title": "Событие не найдено",
  "status": 404,
  "detail": "Событие с Id 3fa85f64-5717-4562-b3fc-2c963f66afa6 не найдено"
}
```
### формат ответа при ошибках "No available seats for the Event"
``` JSON
{
  "title": "Incorrect request incoming parameter value",
  "status": 409,
  "detail": "No available seats for the Event 826d41e3-7e06-4115-a253-4c836583d98a",
  "instance": "/events/826d41e3-7e06-4115-a253-4c836583d98a/book"
}
```
 ### формат ответа при успешном создании бронирования
 ``` JSON
 {
  "id": "ba45e8c6-d038-4f2a-ac18-3081288cbda1",
  "eventId": "d33ae92d-501f-4bd6-8056-a3bb1f36fd0f",
  "status": "Pending"
}
 ```

















 
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
* **ответ в случае отсутствия события:** `404 OK`
```json
{
  "title": "Resource not found",
  "status": 404,
  "detail": "Событие с идентификатором 'd23ae92d-501f-4bd6-8056-a3bb1f36fd0f' не найдено.",
  "instance": "/events/d23ae92d-501f-4bd6-8056-a3bb1f36fd0f"
}
```










## POST /events — создать событие, возвращать корректный HTTP-ответ (например, 201)
-> метод контроллера CreateEvent([FromBody] EventDto eventDto)
    -> метод сервиса событий CreateEvent(EventDto eventDto)
        -> фабричный метод Event Create(EventDto eventDto)
* **Тело запроса:**
```json
{
  "title": "string",
  "description": "string",
  "startAt": "2026-09-14T08:26:11.257Z",
  "endAt": "2026-10-14T08:26:11.257Z",
  "totalSeats": 150
}
```
* **Проверка валидации входных данных:**
  * title — заголовок события , обязательное поле, не может быть пустым
  * description — описание события ,необязательное поле
  * startAt — дата и время начала события , обязательное поле, должно быть меньше endAt
  * endAt — дата и время окончания события , обязательное поле, должно быть больше startAt
  * totalSeats - общее число мест, обязательное поле, должно быть >= 0
* **Метод:** `POST`
* **URL:** `/events`
* **Успешный ответ:** `201 Created`
* **Тело ответа:**
```json
{
  "id": "ae59ebe6-b629-45ca-b141-5635cd96f7fb",
  "title": "string",
  "description": "string",
  "startAt": "2026-09-14T08:26:11.257Z",
  "endAt": "2026-10-14T08:26:11.257Z",
  "totalSeats": 150,
  "availableSeats": 150
}
```
* **Неуспешный ответ (валидация):** `400 Bad Request`
* **Тело запроса**
```JSON
{
  "title": "string",
  "description": "string",
  "startAt": "2026-09-14T08:26:11.257Z",
  "endAt": "2026-10-14T08:26:11.257Z",
  "totalSeats": null
}
```
* **Тело ответа:**
```JSON
{
  "title": "Validation Error occurred",
  "status": 400,
  "instance": "/events",
  "errors": {
    "TotalSeats": [
      "Необходимо указать общее количество мест на событии"
    ]
  }
}
```
















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
* **ответ в случае ошибки - отсутсвие события:** `404 Not Found`
* **Тело ответа:**
```json
{
  "title": "Resource not found",
  "status": 404,
  "detail": "Событие с идентификатором d32ae92d-501f-4bd6-8056-a3bb1f36fd0f не найдено.",
  "instance": "/events/d32ae92d-501f-4bd6-8056-a3bb1f36fd0f"
}
```














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
* **ответ в случае ошибки - отсутсвие события:** `404 Not Found`
* **Тело ответа:**
```json
{
  "title": "Resource not found",
  "status": 404,
  "detail": "Событие с идентификатором d33ae92d-501f-4bd6-8056-a3bb1f36fd0f не найдено.",
  "instance": "/events/d33ae92d-501f-4bd6-8056-a3bb1f36fd0f"
}
```












 ## возвращает текущее состояние брони по её идентификатору
* **Метод:** `GET`
* **URL:** `/bookings/{bookingId}`
* **Успешный ответ:** `200 OK`
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
если в процессе работы фонового процесса обработки бронирования возникла исключительные события, например, событие было удалено, нет свободных мест на текущее событие, или другие ошибки,
то статус обекта бронирования изменить на 3 (Rejected).
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





## Модели (папка Models):
### модель Booking - сущность бронирования события
**Состав полей:**
- Id (Guid, обязательное) — уникальный идентификатор брони; Система генерирует id при создании бронирования
- EventId (Guid, обязательное) — идентификатор события, к которому относится бронь, - суть FK
- Status (BookingStatus, обязательное) — текущий статус брони;
- CreatedAt (DateTime, обязательное) — дата и время создания брони;
- ProcessedAt (DateTime?, опциональное) — дата и время обработки брони.
- public void Confirm() - метод меняет Status со значением Pending в Status со значнием Confirmed, фиксирует время изменения статуса в свойстве ProcessedAt
- public void Reject() - метод меняет Status со значением Pending в Status со значнием Rejected, фиксирует время изменения статуса в свойстве ProcessedAt

### модель enum BookingStatus со значениями:
- Pending = 1  — бронь создана, ожидает обработки;
- Confirmed = 2 — бронь подтверждена;
- Rejected = 3 — бронь отклонена.

### модель BookingDto - используется для передачи данных о бронировании при обновлении в хранилище бронирований
** Состав полей: ** 
- BookingStatus Status - аналогично полю Booking
- DateTime? ProcessedAt - аналогично полю Booking

### модель BookingRequest - используется при создании бронирования в хранилище бронирований в классе InMemoryBookingDictionary в методе CreateBooking(BookingRequest bookingRequest)
** Состав полей: ** 
- EventId (Guid) — идентификатор события, к которому относится бронь;

### модель BookingResponse - используется как модель возврата объекта брони из хранилища в методе контроллера CreateBookingAsync(Guid eventId), в методе сервиса бронирования GetBookingByIdAsync(Guid bookingId) 
** Состав полей: **
- BookingId (Guid) - уникальный id объекта бронирования в хранилище бронировний
- EventId (Guid) - идентификатор события, к которому относится бронь;
- Status (BookingStatus) — текущий статус брони;


### модель Event - сущность события
** Состав полей: **
- Guid Id - уникальный идентификатор события в хранилище событий 
- string Title - заголовок события 
- string? Description - описание события
- DateTime StartAt - дата и время начала события
- DateTime EndAt - дата и время окончания события
- int TotalSeats - общее число мест события
- int AvailableSeats - число совободных мест 
- Event Create(EventDto eventDto) - фабричный метод создания события, валидирует обязательное поле TotalSeats : значение должно быть > 0 
- bool TryReserveSeats(int count = 1) - метод резервирования мест. Если свободных место AvailableSeats достаточно для резервирования запрошенного количества, то возвращается true
- void ReleaseSeats(int count = 1) - метод отката резервирования запрошенного количества мест.
- List<Booking> Bookings - навигационное свойство для связи с сущностью Booking

### модель EventDto - используется как модель входных данных в методе CreateEvent([FromBody] EventDto eventDto) контроллера, в методе CreateEvent(EventDto eventDto) сервиса событий
** Состав полей: **
- string Title - заголовок события, обязательное поле (required) 
- string? Description - описание события, необязательное поле 
- DateTime StartAt - дата и время начала события, обязательное поле (required) 
- DateTime EndAt - дата и время окончания события, обязательное поле (required) 
- int TotalSeats - общее число мест события, обязательное поле (required) 
Реализует IValidatableObject, для валидирования StartAt и EndAt в привязке: Дата начала события должна быть меньше даты окончания события

### модель EventResponse - используется как модель возврата объекта события из хранилища событий в сервисе событий в методе GetEventResponseById(Guid id) - метод для контролеров, в методе сервиса Событий CreateEvent(EventDto eventDto), в методе PaginatedResult GetAllEvents 
** Состав полей: **
- Guid Id - уникальный идентификатор события в хранилище событий 
- string Title - заголовок события 
- string? Description - описание события
- DateTime StartAt - дата и время начала события
- DateTime EndAt - дата и время окончания события
- int TotalSeats - общее число мест события
- int AvailableSeats - число совободных мест

### модель PaginatedResult - модель возврата пагинированных событий из хранилища событий
** Состав полей: **
- int totalEvents - число событий на странице результата
- EventResponse[] eventArray - массив EventResponse на странице результата
- int currentPage - номер страницы
- int pageSizeOfCurrentPage - размер страницы




# Сервисы

## Сервис событий EventService
- ConcurrentDictionary<Guid,Event> _events - хранилище событий
- ConcurrentDictionary<Guid, SemaphoreSlim> _semaphoreOfEventId = new(); - словарь семафоров с ключами-идентификаторами событий. Для атомарного резервирования места по Id события в букинг-сервисе и для атомарного отката события при возникновении ошибки во время обработки букинга в фоновом процессе
- Task<bool> IsExistInRepositoryAsync(Guid id, CancellationToken ct) - асинхронный метод для проверки сущещствования объекта события с Id. Метод иммитирует обращение к БД и принимает токен отмены. Возвращает булево значение
- PaginatedResult GetAllEvents(string? title, DateTime? from, DateTime? to,int page = 1, int pageSize = 10) - метод получения событий с пагинацией. метод возвращает результаты из хранилища событий , фильтрует их
        по заголовку (title), по Дате и времени начала события (from), по дате и времени окончания события (to), из отфильтрованной коллекции возвращает страницу с номером page, где каждая страница -- размера pageSize. По умолчанию
        page = 1 , pageSize = 10
- EventResponse GetEventResponseById(Guid id) - Возвращает сущность EventResponse, если в хранилище есть объект Event с переданным Id. Выбрасывает исключение KeyNotFoundException в случае отстутвия объекта Event с Id в хранилище.метод для контроллеров.
- Event GetEventById(Guid id) - Возвращает сущность Event, если в хранилище есть объект Event с переданным Id. Выбрасывает исключение KeyNotFoundException в случае отстутвия объекта Event с Id в хранилище. метод для бизнес-логики в других сервисах -- возвращает Event
- EventResponse CreateEvent(EventDto eventDto)  - метод создания сущности Event. бросает исключение ValidationException, если eventDto.TotalSeats!.Value < 0
- public async Task UpdateEvent(Guid id, EventDto eventDto)  - метод обновления события. принимает в качестве параметра Id события для обновления, EventDto,CancellationToken. метод для контроллеров.
- void UpdateEventById(Guid id, Event Event) - метод обновления события. принимает в качестве параметра Id события для обновления, Event. метод для бизнес-логики в других сервисах
- void DeleteEvent(Guid id) - метод удаления события.
- async Task ReleaseSeatAsync(Guid eventId) - метод для фонового процесса. метод отката.
- async Task TryReserveSeats(Guid eventId) - атомарно резервирует одно место для события для букинг-сервиса.
- private AppDbContext _context -- контекс БД.


## Сервис бронирований BookingService
- private readonly IBookingRepository _bookingStorage - объект хранилища объектов бронирований
- private readonly IEventService _eventService - сервис событий
- private readonly object _bookingLock = new() - объект для синхронизации для защиты критической секции 
- Task<BookingResponse> CreateBookingAsync(Guid eventId) - асинхронный метод создания бронирования. для защиты критической секции использует объект _bookingLock. критическая секция: проверка существования события с помощью _eventService, резервирование билетов на событие
        выбрасывает исключение KeyNotFoundException в случае отсутсвия события с переданным Id. Выбрасывает исключение NoAvailableSeatsException, если нет доступных мест для резервирования.
- Task<BookingResponse> GetBookingByIdAsync(Guid bookingId) - метод возврата объекта бронирования BookingResponse из хранилища по его Id

## Сервис фоновой обработки бронирований BookingBackgroundService
- IBookingRepository _bookingRepository - хранилище бронирований
- IEventService _eventRepository - хранилище событий, сервис событий
- SemaphoreSlim _processingSemaphore = new(1, 1) - семафор для защиты критической секции. Критическая секция: проверка существования события бронирования, обработка бронирования, обновление объекта события в хранилище событий, обновление объекта бронирования в хранилище бронирований.
- Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken) - асинхронный метод обработки одной брони. Обрабатывает следующие исключения: OperationCanceledException, ошибки обновления объекта бронирования 
- Task ExecuteAsync(CancellationToken stoppingToken) - обрабатывает асинхронно все объекты бронирования со статусом Pending. Меняет статус броинрования с Pending на Confirmed или Reejcted.
## Хранилище броинирований.
- ConcurrentDictionary<Guid, Booking> _bookings = new() - словарь для хранения объектов бронирований
- IEnumerable<Booking> GetPending() - метод возвращает все объекты бронирований со статусов Pending
- BookingResponse[] GetAllBookings() - метод возварщает все объекты броинрований из хранилища. Если в хранилище пустое, то возварщает пустой массив.
- PaginatedResultBookings GetAllBookingsPaginated - метод, аналогичный методу PaginatedResult GetAllEvents в EventService
- BookingResponse GetBookingById(Guid bookingId) - метод возвращает объект бронирования из хранилища по его Id. Выбрасывает KeyNotFoundException, если объекта bookingId нет в хранилище
- void UpdateBooking(Guid bookingId, BookingDto bookingDto) - метод обновляет объект бронирования с Id = bookingId данными из bookingDto.
- void DeleteBooking(Guid bookingId) - удаляет объект с id= bookingId из хранилища, выбрасывает KeyNotFoundException, если объекта с bookingId нет в хранилище
- BookingResponse CreateBooking(BookingRequest bookingRequest) - создает объект бронирования в хранилище с уникальным Id, возвращает объект с информацией о созданном бронировании. 

# Контекст БД
- содержит две таблицы: Events и Bookings
- метод OnModelCreating связывает все конфигурации к контексту.
- DbContext подключается к провайдеру postgreSQL. Строка подключения -- из appsettings.json. DbContext регистрируется как scoped.




# Конфигурация (DataAccess\Configurations)
- все конфигурации настроены отдельно от самих сущностей, а подключаются автоматически из сборки в OnModelCreating
- конфигурация настроена в EventConfiguration : IEntityTypeConfiguration<Event>
    - Id - PK без генерации значения
    - Title - обязательное поле, с максимальной длиной значения 200
    - Description - необязательное поле с максимальной длиной значения 2000
    - StartAt - обязательное поле
    - EndAt - обязательное поле
    - TotalSeats - обязательное поле
    - настроена связь с сущностью Bookings 1:N
- конфигурация настроена в BookingConfiguration : IEntityTypeConfiguration<Booking>
    - Id - PK без генерации значения
    - хранение Status как строки в БД



## глобальная обработка ошибок через кастомный Middleware.
- Все ошибки возвращаются в стандартизированном JSON-формате по спецификации Problem Details:
- 400 Bad Request — ошибки валидации входных данных
- 404 Not Found — запрашиваемое событие с указанным id не найдено
- 500 Internal Server Error — непредвиденные сбои на стороне сервера





## пример сценария использования
запрос на создание бронирования события с Id 317f1592-9109-4050-a6ea-a8ada60dcaf0:
```https://localhost:7087/events/317f1592-9109-4050-a6ea-a8ada60dcaf0/book
запрос на получение статуса бронирования с Id 8e318042-0986-46cd-a641-2d4ec6769b4c:
```https://localhost:7087/bookings/8e318042-0986-46cd-a641-2d4ec6769b4c






## потокобезопасность при конкурентных запросах и предотвращение овербукинга
Метод контроллера CreateEvent принимает значение EventDto с объязательным свойством totalSeats. 
totalSeats валидируется автоматически в привязке модели. 


# Синхронизация
- в сервисе Событий есть поле private ConcurrentDictionary<Guid, SemaphoreSlim> _semaphoreOfEventId = new() -- словарь семафоров,
где ключом является Id события. Логика такая. Несколько потоков с запросом на создание букинга по eventId обращаются 
к сервису событий на резервирование одного билета на один и тот же eventId, и все эти потоки вызывают метод TryReserveAsync (eventId). Поток в методе TryReserveAsync (eventId) 
захватывает семафор из словаря _semaphoreOfEventId по eventId. Далее проверка события на наличие, обновление объекта события и сохранение события в хранилище, то есть все операции
от получения объекта события до сохрания его обновленного состояния в хранилище происходят атомарно.
- в сервисе фоновой обработки бронирований BookingBackgroundService используется SemaphoreSlim _processingSemaphore = new(1, 1) для защиты критической секции. 
Критическая секция - это проверка на наличие объекта события для броинрования, то есть чтение в хранилище событий, обновление объекта бронирования в хранилище бронирований
, откат события в случае ошибки, отличной от OperationCanceledException - вызов метода ReleaseSeatAsync. в методе ReleaseSeatAsync так же захватывается семафор, но для защиты другой критической 
секции -- объекат события конкретного id.  


