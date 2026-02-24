# Weatherapp
First app on windows

## Unity CarController fix
Dodałem przykładowy `CarController.cs`, który stabilizuje auto i ogranicza typowe "wystrzały" pojazdu do góry.

Najważniejsze zmiany:
- niżej ustawiony środek masy (`centerOfMassOffset`),
- stabilniejsze substeps dla `WheelCollider`,
- limit prędkości i odcięcie momentu napędowego po przekroczeniu limitu,
- `downforce` dociskający auto do podłoża,
- hamulec na `Space` (`brakeTorque`),
- zabezpieczenie `null` przy aktualizacji meshy kół.

## Jak wrzucić to do Unity (krok po kroku)
1. Otwórz swój projekt w Unity.
2. W panelu **Project** wejdź do folderu `Assets` i utwórz np. folder `Scripts`.
3. Skopiuj plik `CarController.cs` do `Assets/Scripts/` (możesz przeciągnąć plik do okna Unity albo wkleić przez eksplorator plików).
4. W scenie zaznacz obiekt auta (ten, który ma `Rigidbody`).
5. Kliknij **Add Component** i dodaj `CarController`.
6. W Inspectorze podepnij wszystkie pola:
   - `WheelCollider_FL/FR/RL/RR` → odpowiednie wheel collidery,
   - `FrontLeftWheel/FrontRightWheel/RearLeftWheel/RearRightWheel` → meshe kół.
7. Sprawdź, czy skala auta i rodziców to `1,1,1`.
8. Upewnij się, że auto na starcie nie przecina podłoża.
9. Uruchom scenę i testuj:
   - `W/S` lub strzałki góra/dół: gaz/cofanie,
   - `A/D` lub strzałki lewo/prawo: skręt,
   - `Space`: hamulec.

## Krytyczny fix (u Ciebie najważniejsze)
Jeśli auto ma tylko `Rigidbody` i **nie ma żadnego Collidera na body**, to fizyka będzie się zachowywać niestabilnie przy kontakcie z plane.

Zrób dokładnie to:
1. Na root auta dodaj `BoxCollider` (albo `MeshCollider` ustawiony rozsądnie, ale na start lepiej `BoxCollider`).
2. Collider body ustaw tak, żeby obejmował karoserię, ale NIE przecinał ziemi na starcie.
3. Koła zostaw jako `WheelCollider` (one nie zastępują collidera karoserii).
4. W `Rigidbody` ustaw:
   - `Mass`: `1200`
   - `Drag`: `0.02`
   - `Angular Drag`: `0.5`
   - `Interpolate`: `Interpolate`
   - `Collision Detection`: `Continuous Dynamic`
5. Na `Plane` upewnij się, że ma `Collider` i nie ma dziwnych skal (najlepiej `1,1,1` albo równe wartości).
6. Sprawdź warstwy w `Project Settings > Physics`, czy warstwa auta koliduje z warstwą podłoża.

## Szybki preset na start (żeby nie latało)
W `CarController` ustaw:
- `motorForce = 900`
- `maxSteerAngle = 25`
- `maxSpeedKmh = 80`
- `downforce = 40`
- `centerOfMassOffset = (0, -0.8, 0)`

## Gdy auto dalej skacze
- Zmniejsz `motorForce` jeszcze niżej (np. `600-700`).
- Zmniejsz `downforce`, jeśli przy większej prędkości auto robi nienaturalne podbicia.
- Sprawdź każdy `WheelCollider`:
  - `radius` zgodny z rozmiarem koła,
  - pozycja Y mniej więcej w środku koła,
  - zawieszenie nie jest skrajnie twarde.
- Wyłącz na chwilę wszystkie dodatkowe skrypty od sił (`AddForce`) poza `CarController`, żeby wykluczyć konflikt.


## Fix odbicia przy pierwszym kontakcie z plane
Jeśli auto spada poprawnie, ale po pierwszym dotknięciu ziemi odbija się i zaczyna wariować:

1. Na colliderze auta i plane ustaw **Physic Material**:
   - `Bounciness = 0`
   - `Bounce Combine = Minimum`
   - `Dynamic Friction = 0.8`
   - `Static Friction = 1`
2. W `Project Settings > Physics` ustaw `Default Contact Offset` na `0.01` (za duży offset daje "sprężynowanie").
3. Upewnij się, że żaden collider w aucie nie nachodzi na plane w klatce startowej.
4. W nowym `CarController` są 2 parametry na ten problem:
   - `landingVerticalDamping` (np. `0.1-0.3`) tłumi podbicie po lądowaniu,
   - `maxAirAngularSpeed` (np. `2-3`) ogranicza koziołkowanie w powietrzu.
5. Na test wyłącz `downforce` (ustaw `0`) i sprawdź, czy to on nie dokłada niestabilności przy dotknięciu ziemi.


## Dodatkowa poprawka na odbicie (po Twoim feedbacku)
Jeśli po ustawieniu `downforce = 0` dalej odbija, to problem najczęściej siedzi w WheelCollider/suspension, nie w samym docisku.

W nowym `CarController` dodałem parametry do strojenia bez kodu:
- `suspensionSpring = 25000`
- `suspensionDamper = 4500`
- `suspensionDistance = 0.18`
- `wheelDampingRate = 1.2`
- `forwardFrictionStiffness = 1.4`
- `sidewaysFrictionStiffness = 2.0`
- `landingAssistTime = 0.15`
- `landingVerticalDamping = 0.2`

Dla Twojego przypadku ustaw na start:
1. `downforce = 0`
2. `suspensionSpring = 18000`
3. `suspensionDamper = 5500`
4. `landingAssistTime = 0.2`
5. `landingVerticalDamping = 0.1`

Jeśli nadal raz podbija, zmniejsz `suspensionDistance` do `0.14-0.16`.


## Gdy wariuje dokładnie przy dotyku WheelCollider
To prawie zawsze znaczy: **WheelCollider ma złą pozycję albo zły radius** względem wizualnego koła.

W nowym `CarController` dodałem auto-korektę:
- `autoAlignWheelColliders = true`
- skrypt ustawia pozycję `WheelCollider` na pozycję mesha koła
- skrypt liczy `radius` z rozmiaru mesha (`wheelRadiusScale`)

Co zrobić w Unity teraz:
1. Na obiekcie auta zostaw `autoAlignWheelColliders = true`.
2. Ustaw `wheelRadiusScale = 0.95`.
3. Kliknij Play i sprawdź, czy koła nie zaczynają pod ziemią.
4. Jeśli nadal podbija: zmniejsz `wheelRadiusScale` do `0.9`.
5. Ustaw `forceAppPointDistance = 0.02-0.08` (za duże wartości destabilizują).

Szybki test diagnostyczny:
- Ustaw auto 2m nad plane i odpal bez gazu.
- Jeśli po kontakcie dalej odbija: problem jest w geometrii/parametrach WheelCollider, nie w napędzie.
