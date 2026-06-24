from fastapi import FastAPI

from .models import DartOut, ThrowRequest, ThrowResponse
from .strategies.human_like_strategy import HumanLikeStrategy
from .strategies.naive_strategy import NaiveStrategy

app = FastAPI(title="Bullseye Dartbot AI Service")

# accuracy values below were found by simulating full legs at each value and
# picking the one whose resulting 3-dart average lands closest to the
# beginner/semi/pro targets (~40/65/90) - see human_like_strategy.py for what
# "accuracy" actually controls.
_strategies = {
    "beginner": HumanLikeStrategy(accuracy=-0.95),
    "semi": HumanLikeStrategy(accuracy=-0.10),
    "pro": HumanLikeStrategy(accuracy=0.30),
    "naive": NaiveStrategy(),
}


@app.get("/health")
def health() -> dict:
    return {"status": "ok"}


@app.post("/bot/throw", response_model=ThrowResponse)
def throw(request: ThrowRequest) -> ThrowResponse:
    strategy = _strategies.get(request.difficulty.lower(), _strategies["beginner"])
    result = strategy.decide_turn(request.remainingScore, request.variant)

    return ThrowResponse(
        totalPoints=result.total_points,
        darts=[DartOut(segment=d.segment, multiplier=d.multiplier, points=d.points) for d in result.darts],
    )
