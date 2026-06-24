from pydantic import BaseModel


class ThrowRequest(BaseModel):
    remainingScore: int
    variant: str = "501"
    difficulty: str = "beginner"  # "beginner" | "semi" | "pro"


class DartOut(BaseModel):
    segment: str
    multiplier: int
    points: int


class ThrowResponse(BaseModel):
    totalPoints: int
    darts: list[DartOut]
