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
git clone -b sprint-6 https://github.com/michaelchemmsu-eng/events_api.git
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
- асинхронный метод получения списка событий поддерживает следующие опциональные параметры строки запроса (Query parameters):
- string title  -- Поиск по названию события
- DateTime from -- Фильтр по дате: события, которые начинаются не раньше указанной даты (StartAt >= from)
- DateTime to -- Фильтр по дате: события, которые заканчиваются не позже указанной даты (EndAt <= to)
- int page -- Номер страницы (по умолчанию: 1), фильтрация по значению: page должен быть больше нуля
- int pageSize -- Количество событий на странице (по умолчанию: 10),  фильтрация по значению: pageSize должен лежать в диапазоне [1,100]
- CancellationToken ct -- токен отмены
- примеры запросов:
    - GET /api/events?title=тайтл
    - GET /api/events?from=2025-06-01T00:00:00Z&to=2025-06-30T23:59:59Z
    - GET /api/events?page=2&pageSize=5
- Формат ответа запроса при стандартных значениях параметров:
```JSON
{
  "totalEvents": 0,
  "eventArray": [],
  "currentPage": 1,
  "pageSizeOfCurrentPage": 0
}
```
- обращается к сервису событий EventService, вызывает метод 
    GetAllEventsPaged (string? title, DateTime? from, DateTime? to,int page = 1, int pageSize = 10, CancellationToken ct = default) - 
    метод получения событий с пагинацией. метод возвращает результаты из хранилища событий , фильтрует их
    по заголовку (title), по Дате и времени начала события (from), 
    по дате и времени окончания события (to), 
    из отфильтрованной коллекции возвращает страницу с номером page, где каждая страница -- размера pageSize. По умолчанию
    page = 1 , pageSize = 10. 












## Создание бронирования для события c идентициактором eventId (`POST /events/{eventId}/book`)
- Метод создания бронирования поддерживает параметр eventId типа Guid - идентификатор события, для которого создается бронирование. возвращает 202 Accepted;  
- теле ответа возвращает информацию о созданной брони (включая Id, EventId, Status);
- в заголовке Location возвращает ссылку на ресурс брони (например, /bookings/{bookingId});
пример запроса:
- POST /events/3fa85f64-5717-4562-b3fc-2c963f66afa6/book
- BookingService.CreateBookingAsync() ->
     Booking.Create()
     EventRepository.AddEventAsync()
     EventRepository.SaveChangesAsync()
     Про метод EventRepository.SaveChangesAsync(). Свойство Event.AvailableSeats в EventConfigurations объявлено , как IsConcurrencyToken. метод EventRepository.SaveChangesAsync() 
     вызывает метод DbContext.SaveChangesAsync() и может завершиться с исключением DbUpdateConcurrencyException, если свойство AvailableSeats в БД было изменено в момент сохранения
     . EventRepository.SaveChangesAsync() перехватывает это исключение, получает entity, из-за которого метод SaveChangesAsync() завершился с исключением, получает текущие значения
     свойств этого entity из БД, и , если при этом вернулось значение null, значит, объекта события нет в БД, запись о нет была удалилена другим пользователем - в таком случае метод 
     EventRepository.SaveChangesAsync() завершается с исключением KeyNotFoundException. Если же получили из БД значения свойств, то в таком случае нас интересует число уже купленных
     билетов на это событие. При обновлении нужно учитывать это число, чтобы при сохранении не потерять уже купленные билеты. Если новое значение TotalSeats меньше числа купленных билетов
     , то в таком случае EventRepository.SaveChangesAsync() завершается с исключением NoAvailableSeatsException, иначе корректируем AvailableSeats и OriginalValues объекта EntityEntry.

    

- формат ответа при ошибке "событие не найдено" (KeyNotFoundException):
    ``` JSON
    {
      "title": "Событие не найдено",
      "status": 404,
      "detail": "Событие с Id 3fa85f64-5717-4562-b3fc-2c963f66afa6 не найдено"
    }
    ```
- формат ответа при ошибках "No available seats for the Event"
``` JSON
{
  "title": "Incorrect request incoming parameter value",
  "status": 409,
  "detail": "No available seats for the Event 826d41e3-7e06-4115-a253-4c836583d98a",
  "instance": "/events/826d41e3-7e06-4115-a253-4c836583d98a/book"
}
```
- формат ответа при успешном создании бронирования
 ``` JSON
 {
  "id": "ba45e8c6-d038-4f2a-ac18-3081288cbda1",
  "eventId": "d33ae92d-501f-4bd6-8056-a3bb1f36fd0f",
  "status": "Pending"
}
 ```
 

















 
## получить событие по id; если не найдено — вернуть корректный HTTP-ответ
- метод контроллера принимает Guid id события и токен отмены.
- обращается **GetEventResponseById** сервиса событий. 
  метод сервиса событий вызывает **GetEventByIdAsync** репозитория событий. 
  Если событие не найдено, то бросает исключение **KeyNotFoundException**, 
  иначе возвращает объект **EventResponse**, описывающий данное событие
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
  "title": "Validation Error occurred",
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
* **Тело ответа:**
```JSON
{
  "title": "incorrect request",
  "status": 400,
  "detail": "Validation error: a value for Total Seats must be positive",
  "instance": "/events"
}
```
* **Тело ответа:**
```JSON
{
  "title": "Validation Error occurred",
  "status": 400,
  "instance": "/events",
  "errors": {
    "Title": [
      "Название события обязательно"
    ]
  }
}
```
* **Тело ответа:**
```JSON
{
  "title": "Validation Error occurred",
  "status": 400,
  "instance": "/events",
  "errors": {
    "EndAt": [
      "Дата окончания события обязательно"
    ]
  }
}
```
* **Тело ответа:**
```JSON
{
  "title": "Validation Error occurred",
  "status": 400,
  "instance": "/events",
  "errors": {
    "EndAt": [
      "Дата окончания события обязательно"
    ],
    "StartAt": [
      "Дата начала события обязательно"
    ]
  }
}
```




















## обновить событие целиком; если не найдено — вернуть корректный HTTP-ответ (например, 404)
- метод принимает в качестве аргумента Guid, EventDto, CancellationToken. Метод обновляет в БД данные объекта события с переданным в качестве аргумента Id
    данными объекта EventDto, переданного также в качестве аргумента. При этом значение столбца AvailableSeats обновляется значением свойства TotalSeats объекта EventDto
- EventService.UpdateEventAsync ->
    EventRepository.IsExistInRepositoryAsync ->
        DbContext.Events.AnyAsync
    Event.Create
    EventRepository.UpdateEvent
    EventRepository.SaveChangesAsync
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
* **ответ в случае ошибки - новое значение TotalSeats невалидно:** `409`
* **Тело ответа:**
```json
{
  "title": "Incorrect request incoming parameter value",
  "status": 409,
  "detail": "Total seats cannot be less than the number of seats already taken",
  "instance": "/events/63ddd565-e8eb-477c-9518-7877130190b6"
}
```


- ошибки валидации в привязке такие же, как в методе POST /events
- ошибки валидации при создании Event из EventDto такие же, как в методе POST /events













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
* **ответ в случае ошибки на стороне БД - Нарушение FK ограничения:** `409 Conflict`
* **Тело ответа:**
```json
{
  "title": "Incorrect request incoming parameter value",
  "status": 409,
  "detail": "Невозможно удалить событие с ID 04788b33-3cc2-450c-91bb-3171d1692bb8, так как на него уже существуют бронирования.",
  "instance": "/events/04788b33-3cc2-450c-91bb-3171d1692bb8"
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









# Модели (папка Models):
## модель Booking
- сущность бронирования события
**Состав полей:**
- Id (Guid, обязательное) — уникальный идентификатор брони; Система генерирует id при создании бронирования
- EventId (Guid, обязательное) — идентификатор события, к которому относится бронь, - суть FK
- Status (BookingStatus, обязательное) — текущий статус брони;
- CreatedAt (DateTime, обязательное) — дата и время создания брони;
- ProcessedAt (DateTime?, опциональное) — дата и время обработки брони.
- public void Confirm() - метод меняет Status со значением Pending в Status со значнием Confirmed, фиксирует время изменения статуса в свойстве ProcessedAt
- public void Reject() - метод меняет Status со значением Pending в Status со значнием Rejected, фиксирует время изменения статуса в свойстве ProcessedAt
- static Booking Create(BookingRequest bookingRequest) - фабричный метод создания объекта сущности Booking.

## модель enum BookingStatus со значениями:
- Pending = 1  — бронь создана, ожидает обработки;
- Confirmed = 2 — бронь подтверждена;
- Rejected = 3 — бронь отклонена.

## модель BookingDto 
- используется для передачи данных о бронировании при обновлении в хранилище бронирований
** Состав полей: ** 
- BookingStatus Status - аналогично полю Booking
- DateTime? ProcessedAt - аналогично полю Booking

## модель BookingRequest 
- используется при создании бронирования в хранилище бронирований в сервисе бронирований в методе CreateBooking(BookingRequest bookingRequest)
** Состав полей: ** 
- EventId (Guid) — идентификатор события, к которому относится бронь;

## модель BookingResponse 
- используется как модель возврата объекта брони из хранилища в методе контроллера CreateBookingAsync(Guid eventId), в методе сервиса бронирования GetBookingByIdAsync(Guid bookingId) 
** Состав полей: **
- BookingId (Guid) - уникальный id объекта бронирования в хранилище бронировний
- EventId (Guid) - идентификатор события, к которому относится бронь;
- Status (BookingStatus) — текущий статус брони;


## модель Event 
- сущность события
** Состав полей: **
- Guid Id - уникальный идентификатор события в хранилище событий 
- string Title - заголовок события 
- string? Description - описание события
- DateTime StartAt - дата и время начала события
- DateTime EndAt - дата и время окончания события
- int TotalSeats - общее число мест события
- int AvailableSeats - число совободных мест 
- Event Create(EventDto eventDto) - фабричный метод создания события, валидирует обязательное поле TotalSeats : значение должно быть > 0 
- bool ReserveSeats(int count = 1) - метод резервирования мест. Если свободных место AvailableSeats достаточно для резервирования запрошенного количества, то возвращается true
- void ReleaseSeats(int count = 1) - метод отката резервирования запрошенного количества мест.
- List<Booking> Bookings - навигационное свойство для связи с сущностью Booking

## модель EventDto 
- используется как модель входных данных в методе CreateEventAsync([FromBody] EventDto eventDto) контроллера, в методе CreateEventAsync(EventDto eventDto) сервиса событий
** Состав полей: **
- string Title - заголовок события, обязательное поле (required) 
- string? Description - описание события, необязательное поле 
- DateTime StartAt - дата и время начала события, обязательное поле (required) 
- DateTime EndAt - дата и время окончания события, обязательное поле (required) 
- int TotalSeats - общее число мест события, обязательное поле (required) 
Реализует IValidatableObject, для валидирования StartAt и EndAt в привязке: Дата начала события должна быть меньше даты окончания события

## модель EventResponse 
- используется как модель возврата объекта события из хранилища событий в сервисе событий в методе GetEventResponseById(Guid id) - метод для контролеров, 
  в методе сервиса Событий CreateEventAsync(EventDto eventDto)
** Состав полей: **
- Guid Id - уникальный идентификатор события в хранилище событий 
- string Title - заголовок события 
- string? Description - описание события
- DateTime StartAt - дата и время начала события
- DateTime EndAt - дата и время окончания события
- int TotalSeats - общее число мест события
- int AvailableSeats - число совободных мест

## модель PaginatedResult 
- модель возврата пагинированных событий из хранилища событий
** Состав полей: **
- int totalEvents - число событий на странице результата
- EventResponse[] eventArray - массив EventResponse на странице результата
- int currentPage - номер страницы
- int pageSizeOfCurrentPage - размер страницы




# Сервисы

## Сервис событий EventService
- Task<bool> IsExistInRepositoryAsync(Guid id, CancellationToken ct) - асинхронный метод для проверки сущещствования объекта события с Id. 
  Метод иммитирует обращение к БД и принимает токен отмены. Возвращает булево значение.

- Task <PaginatedResult> GetAllEvents(string? title, DateTime? from, DateTime? to,int page = 1, int pageSize = 10, CancellationToken ct = default)
        метод получения событий с пагинацией. метод возвращает результаты из хранилища событий , фильтрует их
        по заголовку (title), по Дате и времени начала события (from), по дате и времени окончания события (to), 
        из отфильтрованной коллекции возвращает страницу с номером page, где каждая страница -- размера pageSize. По умолчанию
        page = 1 , pageSize = 10

- Task<EventResponse> GetEventResponseById(Guid id,CancellationToken ct ) 
  Возвращает сущность EventResponse, если в хранилище есть объект Event с переданным Id. 
  Выбрасывает исключение KeyNotFoundException в случае отстутвия объекта Event с Id в хранилище.

- Task <EventResponse> CreateEventAsync(EventDto eventDto, CancellationToken ct)
  метод создания сущности Event. бросает исключение ValidationException, если eventDto.TotalSeats!.Value < 0

- public async Task UpdateEventAsync(Guid id, EventDto eventDto,CancellationToken ct)
  метод обновления события. принимает в качестве параметра Id события для обновления, EventDto,CancellationToken. 

- Task DeleteEventAsync(Guid id, CancellationToken ct) - метод удаления события.
- async Task ReleaseSeatAsync(Guid eventId) - метод для фонового процесса. метод отката.
- private readonly IEventRepository _eventRepository - репозиторий событий.


## Сервис бронирований BookingService
- private readonly IBookingRepository _bookingRepository - репозиторий объектов бронирования
- private readonly IEventService _eventService - сервис событий
- репозиторий и сервис событий держат один и тот же контекст БД.
- Task<BookingResponse> CreateBookingAsync(Guid eventId,CancellationToken ct  )
  метод создания бронирования. Создает объект бронирования, кладет этот объект в кеш репозитория, обращается к сервису событий и в методе 
  ReserveSeatsAsync сервиса событий резервирует одно место на выбранное
  событие. В методе ReserveSeatsAsync в случае успеха сохраняется контекст БД, в случае исключения контекст БД не сохраняется, а состяния таблиц Bookings и Events остаются 
  консистентыми (проблема потерянного билета), так как не был вызван DbContext.SaveChangesAsync() из-за исключения.
- Task<BookingResponse> GetBookingByIdAsync(Guid bookingId,CancellationToken ct ) - метод возврата объекта бронирования BookingResponse из хранилища по его Id

## Сервис фоновой обработки бронирований BookingBackgroundService
- фоновый сервис запускается вместе с приложением. Он предназначен для обработки объектов Booking в БД, которые имеют статус pending. Главный цикл с интервалом TimePause - приватное поле с типом TimeSpan - проверяет все записи в таблице Bookings на наличие 
  записей со значением Status, равным pending, возвращает из БД все Id таких записей, причем эта операция извлечения списка Id осуществляется в отдельном Scope. 
- Таким образом, в памяти есть список Id объектов Booking. Далее алгоритм разбирает все эти Id в параллельном режиме с максимальной степенью параллелизма, равной 10.
- В цикле Parallel.ForEachAsync(...) алгоритм создает новый scope, делает запрос в ДБ, получает объект Booking.
- Проверяет есть ли в ДБ объект Event, соответствующий этому объекту Booking, меняет статус объекта Booking, сохраняет новое стотяние в БД.
- В ходе этих операций могут возникнуть исключения: пользователь отменил операцию, перед сохранением запись Booking удалилась.
- Если возникли другие ошибки, то нужно откатить бронирование, то есть вернуть одно место в соответсвтующей строке Event в колонке AvailbleSeats. 
- Приватный метод RollBackBookingAsync предназначен для того, чтобы в отдельном Scope откатить бронирование с идентификатором бронирвания и соответсвтующим идентификатором события: в одной транзакции меняет оба состяния -- Booking.Status and Event.AvailableSeats


# Репозитории
## Репозиторий BookingRepository
- private readonly AppDbContext _context; - контекст
- все методы репозитория создают запрос в БД
- public async Task<Booking?> GetBookingByIdAsync(Guid id, CancellationToken ct = default)
- public async Task AddBookingAsync(Booking booking, CancellationToken ct = default)
- public void UpdateBooking(Booking booking)
- public async Task DeleteBookingAsync(Booking booking, CancellationToken ct = default)
- public async Task<List<Booking>> GetBookingsAsync(Guid eventId, CancellationToken ct = default)
- public async Task<List<Booking>> GetAllBookingsFilteredAsync(Specification<Booking> specification, CancellationToken ct = default)
- public async Task<List<Guid>> GetPendingBookingIds(CancellationToken ct = default)
- public async Task SaveChangesAsync(CancellationToken ct=default)
- public async Task RollBackBookingAsync(Guid bookingId, Guid eventId, CancellationToken ct = default)
- 

## Репозиторий EventRepository
- private readonly AppDbContext _context;
- все методы репозитория создают запрос в БД
- public async Task SaveChangesAsync(CancellationToken ct)
- public async Task<bool> IsExistInRepositoryAsync(Guid id, CancellationToken ct = default)
- public async Task<List<Event>> GetEventsAsync(CancellationToken ct = default)
- public async Task<Event?> GetEventByIdAsync(Guid id, CancellationToken ct = default)
- public async Task AddEventAsync(Event Event,CancellationToken ct = default)
- public void UpdateEvent(Event Event)
- public void DeleteEvent(Event Event)
- public async Task<int> CountAsync(Specification<Event> specification, CancellationToken ct = default)
- public async Task<List<Event>> ListEventsFilteredPagedAsync(Specification<Event> specification, CancellationToken ct = default)
- public async Task<bool> TryReserveSeatAsync(Guid eventId, CancellationToken ct = default)


# Контекст БД
- содержит две таблицы: Events и Bookings
- метод OnModelCreating связывает все конфигурации к контексту.
- DbContext подключается к провайдеру postgreSQL. Строка подключения -- из appsettings.json. DbContext регистрируется как scoped.

# MiddleWare
## ExceptionHandlingMiddleware
- Этот MiddleWare перехватывает исключения. В зависимости от вида исключения возвращает ответ Problem Details с корректным статус кодом пользователю


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
    - AvailableSeats - IsConcurrencyToken 
- конфигурация настроена в BookingConfiguration : IEntityTypeConfiguration<Booking>
    - Id - PK без генерации значения
    - хранение Status как строки в БД
 


# глобальная обработка ошибок через кастомный Middleware.
- Все ошибки возвращаются в стандартизированном JSON-формате по спецификации Problem Details:
- 400 Bad Request — ошибки валидации входных данных
- 404 Not Found — запрашиваемое событие с указанным id не найдено
- 500 Internal Server Error — непредвиденные сбои на стороне сервера
- 499 ClientClosedRequest - операция отменена пользователем





# База данных и миграции (EF Core)

В качестве СУБД используется PostgreSQL. Схема базы данных полностью управляется с помощью миграций Entity Framework Core (`project/Migrations`):

- При запуске приложения, а также при инициализации базы данных в интеграционных тестах, схема создаётся и актуализируется автоматически через вызов `Database.Migrate()` / `Database.MigrateAsync()`. Метод `EnsureCreated()` не используется.
- История применённых миграций отслеживается в системной таблице `__EFMigrationsHistory`.
- Конфигурация таблиц и связей описана через Fluent API (`IEntityTypeConfiguration<T>`):
  - Таблица `Events` — хранит мероприятия; поле `AvailableSeats` настроено как токен оптимистичной конкурентности (`ConcurrencyToken`).
  - Таблица `Bookings` — хранит бронирования со связью `1 : N` к `Events` и правилом каскадного удаления `ON DELETE RESTRICT` (`DeleteBehavior.Restrict`), запрещающим удаление события при наличии связанных броней.

## Управление миграциями вручную (EF Core CLI)

- Применение миграций к базе данных:
  - dotnet ef database update --project project

### Команды для работы с миграциями EF Core

- инструмент командной строки `dotnet-ef`:
  - dotnet tool install --global dotnet-ef
- Создание новой миграции:
  - dotnet ef migrations add <ИмяМиграции>
- Применение миграций к базе данных:
  - dotnet ef database update
- Просмотр списка миграций и их статуса
  - dotnet ef migrations list
- Удаление последней неприменённой миграции
  - dotnet ef migrations remove

# Интеграционные тесты

Проект `EventApi.IntegrationTests` содержит интеграционные тесты для проверки работы репозиториев (`EventRepository`, `BookingRepository`) и корректности применения миграций EF Core на реальной СУБД PostgreSQL.

## Требования для запуска
- Для запуска интеграционных тестов на компьютере должен быть установлен и запущен Docker, так как тесты автоматически поднимают контейнер с базой данных с помощью библиотеки `Testcontainers.PostgreSql` (образ `postgres:16-alpine`).

## Особенности реализации интеграционных тестов
- Единый контейнер на коллекцию тестов: 
  все тестовые классы объединены в общую коллекцию xUnit (`ICollectionFixture<PostgresContainerFixture>`) и используют один общий экземпляр контейнера PostgreSQL.
- Применение миграций:
  перед каждым тестом база данных очищается и схема создаётся исключительно через `Database.MigrateAsync()`. Проверяется наличие таблицы `__EFMigrationsHistory`, таблиц `Events` и `Bookings`, а также ограничения внешнего ключа `RESTRICT`.
- Автоматическая очистка: 
  после завершения всех тестов контейнер PostgreSQL автоматически останавливается и удаляется.







## пример сценария использования
запрос на создание бронирования события с Id 317f1592-9109-4050-a6ea-a8ada60dcaf0:
```https://localhost:7087/events/317f1592-9109-4050-a6ea-a8ada60dcaf0/book
запрос на получение статуса бронирования с Id 8e318042-0986-46cd-a641-2d4ec6769b4c:
```https://localhost:7087/bookings/8e318042-0986-46cd-a641-2d4ec6769b4c






